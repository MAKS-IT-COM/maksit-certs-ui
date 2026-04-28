using Microsoft.Extensions.Hosting;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.RuntimeCoordination;

namespace MaksIT.CertsUI.HostedServices;

/// <summary>
/// Uses a short-lived Postgres lease (<see cref="RuntimeLeaseNames.BootstrapCoordinator"/>) so exactly one pod runs
/// coordination DDL + default admin creation; other pods wait until <c>users</c> exist. No long-lived leader role.
/// </summary>
public sealed class InitializationHostedService(
  ILogger<InitializationHostedService> logger,
  IServiceProvider serviceProvider,
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance
) : IHostedService {

  private static readonly TimeSpan BootstrapLeaseTtl = TimeSpan.FromMinutes(5);

  public async Task StartAsync(CancellationToken cancellationToken) {
    const int delayMilliseconds = 2000;
    while (!cancellationToken.IsCancellationRequested) {
      try {
        logger.LogInformation("Running startup coordination (Postgres bootstrap lease)...");

        var holder = runtimeInstance.InstanceId;
        var acquired = await leaseService.TryAcquireAsync(RuntimeLeaseNames.BootstrapCoordinator, holder, BootstrapLeaseTtl, cancellationToken).ConfigureAwait(false);
        if (!acquired.IsSuccess)
          throw new InvalidOperationException(string.Join(", ", acquired.Messages ?? ["Bootstrap lease acquire failed."]));

        if (acquired.Value) {
          try {
            var engineConfig = serviceProvider.GetRequiredService<ICertsEngineConfiguration>();
            await CoordinationTableProvisioner.EnsureAsync(engineConfig.ConnectionString, cancellationToken).ConfigureAwait(false);

            await using var scope = serviceProvider.CreateAsyncScope();
            var identityDomainService = scope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
            await EnsureIdentityAsLeaderAsync(identityDomainService, cancellationToken).ConfigureAwait(false);
          }
          finally {
            var released = await leaseService.ReleaseAsync(RuntimeLeaseNames.BootstrapCoordinator, holder, CancellationToken.None).ConfigureAwait(false);
            if (!released.IsSuccess && logger.IsEnabled(LogLevel.Warning))
              logger.LogWarning("Bootstrap lease release: {Messages}", string.Join("; ", released.Messages ?? []));
          }

          logger.LogInformation("Startup coordination completed (this instance held the bootstrap lease).");
          return;
        }

        await using (var followerScope = serviceProvider.CreateAsyncScope()) {
          var identityFollower = followerScope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
          while (!cancellationToken.IsCancellationRequested) {
            if (await IsClusterIdentityReadyAsync(identityFollower, cancellationToken).ConfigureAwait(false)) {
              logger.LogInformation("Startup coordination completed (another instance bootstrapped identity).");
              return;
            }

            logger.LogInformation("Waiting for bootstrap to finish (checking database)...");
            await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        logger.LogInformation("Startup coordination canceled (host is stopping).");
        throw;
      }
      catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          logger.LogInformation(ex, "Startup coordination aborted while stopping host.");
          throw new OperationCanceledException("Host stopped during startup coordination.", ex, cancellationToken);
        }
        logger.LogError(ex, "Startup coordination failed. Retrying...");
        try {
          await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
          logger.LogInformation("Startup coordination retry wait canceled (host is stopping).");
          throw;
        }
      }
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private static async Task EnsureIdentityAsLeaderAsync(
    IIdentityDomainService identityDomainService,
    CancellationToken cancellationToken
  ) {
    var count = await identityDomainService.CountUsersAsync(cancellationToken).ConfigureAwait(false);
    if (!count.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", count.Messages));

    if (count.Value != 0)
      return;

    var bootstrap = await identityDomainService.EnsureDefaultAdminAsync(cancellationToken).ConfigureAwait(false);
    if (!bootstrap.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", bootstrap.Messages));
  }

  private static async Task<bool> IsClusterIdentityReadyAsync(
    IIdentityDomainService identityDomainService,
    CancellationToken cancellationToken
  ) {
    var count = await identityDomainService.CountUsersAsync(cancellationToken).ConfigureAwait(false);
    if (!count.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", count.Messages));

    return count.Value > 0;
  }
}
