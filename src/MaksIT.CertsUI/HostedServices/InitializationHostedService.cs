using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.Infrastructure;

namespace MaksIT.CertsUI.HostedServices;

/// <summary>
/// Exactly one instance holds <see cref="RuntimeLeaseNames.PrimaryReplica"/> and runs coordination DDL plus identity bootstrap.
/// Other instances wait until the database (and optional shared <c>init</c> marker under <see cref="Configuration.CertsUIEngineConfiguration.DataFolder"/>) shows bootstrap complete, then start without ACME privileges.
/// </summary>
public sealed class InitializationHostedService(
  ILogger<InitializationHostedService> logger,
  IServiceProvider serviceProvider,
  IOptions<Configuration> appSettings,
  PrimaryReplicaGate primaryGate
) : IHostedService {

  public async Task StartAsync(CancellationToken cancellationToken) {
    const int delayMilliseconds = 2000;
    var appLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

    while (!cancellationToken.IsCancellationRequested) {
      try {
        logger.LogInformation("Running startup initialization (primary replica election)...");

        if (await primaryGate.TryAcquirePrimaryLeaseAsync(cancellationToken).ConfigureAwait(false)) {
          primaryGate.StartLeaseRenewal(appLifetime);
          try {
            var engineConfig = serviceProvider.GetRequiredService<ICertsEngineConfiguration>();
            await CoordinationTableProvisioner.EnsureAsync(engineConfig.ConnectionString, cancellationToken).ConfigureAwait(false);

            await using var scope = serviceProvider.CreateAsyncScope();
            var identityDomainService = scope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
            await EnsureIdentityAsLeaderAsync(appSettings.Value, identityDomainService, cancellationToken).ConfigureAwait(false);
          }
          catch {
            await primaryGate.AbandonPrimaryAsync().ConfigureAwait(false);
            throw;
          }

          primaryGate.EnablePrimaryWorkload();
          logger.LogInformation("Startup initialization completed; this instance is the primary replica.");
          return;
        }

        await using (var followerScope = serviceProvider.CreateAsyncScope()) {
          var identityFollower = followerScope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
          var cfg = appSettings.Value;
          while (!cancellationToken.IsCancellationRequested) {
            if (await IsClusterIdentityReadyAsync(cfg, identityFollower, cancellationToken).ConfigureAwait(false)) {
              logger.LogInformation("Startup initialization completed; this instance is a secondary replica.");
              return;
            }

            logger.LogInformation("Waiting for primary replica to finish database bootstrap...");
            await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        logger.LogInformation("Startup initialization canceled (host is stopping).");
        throw;
      }
      catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          logger.LogInformation(ex, "Startup initialization aborted while stopping host.");
          throw new OperationCanceledException("Host stopped during startup initialization.", ex, cancellationToken);
        }
        logger.LogError(ex, "Startup initialization failed. Retrying...");
        try {
          await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
          logger.LogInformation("Startup initialization retry wait canceled (host is stopping).");
          throw;
        }
      }
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private static async Task EnsureIdentityAsLeaderAsync(
    Configuration appSettings,
    IIdentityDomainService identityDomainService,
    CancellationToken cancellationToken
  ) {
    var dataDir = appSettings.CertsUIEngineConfiguration.DataFolder;
    if (!Directory.Exists(dataDir))
      Directory.CreateDirectory(dataDir);

    var initPath = Path.Combine(dataDir, "init");
    if (File.Exists(initPath))
      return;

    var count = await identityDomainService.CountUsersAsync(cancellationToken).ConfigureAwait(false);
    if (!count.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", count.Messages));

    if (count.Value == 0) {
      var bootstrap = await identityDomainService.EnsureDefaultAdminAsync(cancellationToken).ConfigureAwait(false);
      if (!bootstrap.IsSuccess)
        throw new InvalidOperationException(string.Join(", ", bootstrap.Messages));
    }

    await File.WriteAllTextAsync(initPath, string.Empty, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<bool> IsClusterIdentityReadyAsync(
    Configuration appSettings,
    IIdentityDomainService identityDomainService,
    CancellationToken cancellationToken
  ) {
    var dataDir = appSettings.CertsUIEngineConfiguration.DataFolder;
    if (!Directory.Exists(dataDir))
      Directory.CreateDirectory(dataDir);

    var initPath = Path.Combine(dataDir, "init");
    if (File.Exists(initPath))
      return true;

    var count = await identityDomainService.CountUsersAsync(cancellationToken).ConfigureAwait(false);
    if (!count.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", count.Messages));

    if (count.Value > 0) {
      await File.WriteAllTextAsync(initPath, string.Empty, cancellationToken).ConfigureAwait(false);
      return true;
    }

    return false;
  }
}
