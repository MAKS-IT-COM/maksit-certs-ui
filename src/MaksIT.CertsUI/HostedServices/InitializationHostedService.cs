using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Extensions;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.RuntimeCoordination;

namespace MaksIT.CertsUI.HostedServices;

/// <summary>
/// Runs startup initialization (migrations + identity bootstrap) before the API starts serving requests.
/// FluentMigrator runs first on every instance (same pattern as Vault); the bootstrap lease then ensures
/// only one replica performs identity bootstrap against shared <see cref="Configuration.CertsUIEngineConfiguration.DataFolder"/>.
/// </summary>
public sealed class InitializationHostedService(
  ILogger<InitializationHostedService> logger,
  IServiceProvider serviceProvider,
  IOptions<Configuration> appSettings,
  IRuntimeLeaseService runtimeLease,
  IRuntimeInstanceId runtimeInstance
) : IHostedService {

  private static readonly TimeSpan BootstrapLeaseTtl = TimeSpan.FromMinutes(8);

  public async Task StartAsync(CancellationToken cancellationToken) {
    const int delayMilliseconds = 2000;
    var migrationsApplied = false;

    while (!cancellationToken.IsCancellationRequested) {
      try {
        logger.LogInformation("Running startup initialization...");

        // Migrations must run before lease acquisition: app_runtime_leases is created by FluentMigrator.
        if (!migrationsApplied) {
          await serviceProvider.EnsureCertsEngineMigratedAsync().ConfigureAwait(false);
          migrationsApplied = true;
        }

        var holder = runtimeInstance.InstanceId;
        var acquired = await runtimeLease.TryAcquireAsync(RuntimeLeaseNames.Bootstrap, holder, BootstrapLeaseTtl, cancellationToken).ConfigureAwait(false);
        if (!acquired.IsSuccess)
          throw new InvalidOperationException(string.Join(", ", acquired.Messages ?? ["Lease acquire failed."]));
        if (!acquired.Value) {
          logger.LogInformation("Bootstrap lease held by another instance; waiting...");
          await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
          continue;
        }

        try {
          await using var scope = serviceProvider.CreateAsyncScope();
          var identityDomainService = scope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
          await EnsureIdentityInitializedAsync(appSettings.Value, identityDomainService, cancellationToken).ConfigureAwait(false);
        }
        finally {
          var released = await runtimeLease.ReleaseAsync(RuntimeLeaseNames.Bootstrap, holder, cancellationToken).ConfigureAwait(false);
          if (!released.IsSuccess)
            logger.LogWarning("Bootstrap lease release reported failure: {Messages}", string.Join("; ", released.Messages ?? []));
        }

        logger.LogInformation("Startup initialization completed.");
        return;
      }
      catch (Exception ex) {
        logger.LogError(ex, "Startup initialization failed. Retrying...");
        await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
      }
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private static async Task EnsureIdentityInitializedAsync(
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
}
