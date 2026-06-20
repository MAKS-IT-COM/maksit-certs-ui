using System.Diagnostics;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.HAMode.Abstractions;
using MaksIT.CertsUI.Infrastructure;

namespace MaksIT.CertsUI.HostedServices;

public sealed class InitializationHostedService(
  ILogger<InitializationHostedService> logger,
  IServiceProvider serviceProvider,
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance,
  CertsStartupState startupState
) : IHostedService {

  private static readonly TimeSpan BootstrapLeaseTtl = TimeSpan.FromMinutes(5);

  private readonly Stopwatch _bootstrapStopwatch = Stopwatch.StartNew();

  public async Task StartAsync(CancellationToken cancellationToken) {
    await Initialize(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }

  private async Task Initialize(CancellationToken cancellationToken) {
    const int delayMilliseconds = 2000;
    ((IDatabaseStartupObserver)startupState).OnPhaseStarted(CertsApplicationStartupPhases.BootstrapCoordination);
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

            await using (var scope = serviceProvider.CreateAsyncScope()) {
              var identityDomainService = scope.ServiceProvider.GetRequiredService<IIdentityDomainService>();
              var bootstrap = await identityDomainService.InitializeAdminAsync().ConfigureAwait(false);
              if (!bootstrap.IsSuccess)
                throw new InvalidOperationException(string.Join(", ", bootstrap.Messages ?? []));
            }
          }
          finally {
            var released = await leaseService.ReleaseAsync(RuntimeLeaseNames.BootstrapCoordinator, holder, CancellationToken.None).ConfigureAwait(false);
            if (!released.IsSuccess && logger.IsEnabled(LogLevel.Warning))
              logger.LogWarning("Bootstrap lease release: {Messages}", string.Join("; ", released.Messages ?? []));
          }

          CompleteBootstrapCoordination("this instance held the bootstrap lease");
          return;
        }

        await using (var followerScope = serviceProvider.CreateAsyncScope()) {
          var userAuthFollower = followerScope.ServiceProvider.GetRequiredService<IUserAuthorizationPersistenceService>();
          while (!cancellationToken.IsCancellationRequested) {
            if (await IsClusterIdentityReadyAsync(userAuthFollower, cancellationToken).ConfigureAwait(false)) {
              CompleteBootstrapCoordination("another instance bootstrapped identity");
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

        ((IDatabaseStartupObserver)startupState).OnPhaseFailed(CertsApplicationStartupPhases.BootstrapCoordination, _bootstrapStopwatch.Elapsed, ex);
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

  private void CompleteBootstrapCoordination(string reason) {
    startupState.MarkBootstrapCoordinationComplete(_bootstrapStopwatch.Elapsed);
    var snapshot = startupState.GetSnapshot();
    logger.LogInformation(
      "Startup coordination completed ({Reason}). Application ready after {TotalMs} ms (phase={Phase}).",
      reason,
      (int)snapshot.TotalElapsed.TotalMilliseconds,
      snapshot.CurrentPhase);
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
