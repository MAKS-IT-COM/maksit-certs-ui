using Microsoft.Extensions.Hosting;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;

namespace MaksIT.CertsUI.HostedServices;

public sealed class InitializationHostedService(
  ILogger<InitializationHostedService> logger,
  IServiceProvider serviceProvider,
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance
) : IHostedService {

  private static readonly TimeSpan BootstrapLeaseTtl = TimeSpan.FromMinutes(5);

  private readonly ILogger<InitializationHostedService> _logger = logger;
  private readonly IServiceProvider _serviceProvider = serviceProvider;
  private readonly IRuntimeLeaseService _leaseService = leaseService;
  private readonly IRuntimeInstanceId _runtimeInstance = runtimeInstance;

  public async Task StartAsync(CancellationToken cancellationToken) {
    await Initialize(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }

  private async Task Initialize(CancellationToken cancellationToken) {
    const int delayMilliseconds = 2000;
    while (!cancellationToken.IsCancellationRequested) {
      try {
        _logger.LogInformation("Running startup coordination (Postgres bootstrap lease)...");

        var holder = _runtimeInstance.InstanceId;
        var acquired = await _leaseService.TryAcquireAsync(RuntimeLeaseNames.BootstrapCoordinator, holder, BootstrapLeaseTtl, cancellationToken).ConfigureAwait(false);
        if (!acquired.IsSuccess)
          throw new InvalidOperationException(string.Join(", ", acquired.Messages ?? ["Bootstrap lease acquire failed."]));

        if (acquired.Value) {
          try {
            var engineConfig = _serviceProvider.GetRequiredService<ICertsEngineConfiguration>();
            await CoordinationTableProvisioner.EnsureAsync(engineConfig.ConnectionString, cancellationToken).ConfigureAwait(false);

            await using (var scope = _serviceProvider.CreateAsyncScope()) {
              var identityDomainService = scope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
              var bootstrap = await identityDomainService.InitializeAdminAsync().ConfigureAwait(false);
              if (!bootstrap.IsSuccess)
                throw new InvalidOperationException(string.Join(", ", bootstrap.Messages ?? []));
            }
          }
          finally {
            var released = await _leaseService.ReleaseAsync(RuntimeLeaseNames.BootstrapCoordinator, holder, CancellationToken.None).ConfigureAwait(false);
            if (!released.IsSuccess && _logger.IsEnabled(LogLevel.Warning))
              _logger.LogWarning("Bootstrap lease release: {Messages}", string.Join("; ", released.Messages ?? []));
          }

          _logger.LogInformation("Startup coordination completed (this instance held the bootstrap lease).");
          return;
        }

        await using (var followerScope = _serviceProvider.CreateAsyncScope()) {
          var userAuthFollower = followerScope.ServiceProvider.GetRequiredService<IUserAuthorizationPersistenceService>();
          while (!cancellationToken.IsCancellationRequested) {
            if (await IsClusterIdentityReadyAsync(userAuthFollower, cancellationToken).ConfigureAwait(false)) {
              _logger.LogInformation("Startup coordination completed (another instance bootstrapped identity).");
              return;
            }

            _logger.LogInformation("Waiting for bootstrap to finish (checking database)...");
            await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        _logger.LogInformation("Startup coordination canceled (host is stopping).");
        throw;
      }
      catch (Exception ex) {
        if (cancellationToken.IsCancellationRequested) {
          _logger.LogInformation(ex, "Startup coordination aborted while stopping host.");
          throw new OperationCanceledException("Host stopped during startup coordination.", ex, cancellationToken);
        }
        _logger.LogError(ex, "Startup coordination failed. Retrying...");
        try {
          await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
          _logger.LogInformation("Startup coordination retry wait canceled (host is stopping).");
          throw;
        }
      }
    }
  }

  private static Task<bool> IsClusterIdentityReadyAsync(
    IUserAuthorizationPersistenceService userAuthorizationPersistence,
    CancellationToken cancellationToken
  ) {
    cancellationToken.ThrowIfCancellationRequested();
    var ids = userAuthorizationPersistence.ReadGlobalAdminUserIds();
    return Task.FromResult(ids.IsSuccess && ids.Value != null && ids.Value.Count > 0);
  }
}
