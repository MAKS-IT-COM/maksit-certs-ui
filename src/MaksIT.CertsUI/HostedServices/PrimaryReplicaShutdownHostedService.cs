using MaksIT.CertsUI.Infrastructure;

namespace MaksIT.CertsUI.HostedServices;

/// <summary>
/// Registered last so <see cref="IHostedService.StopAsync"/> runs first on shutdown: releases the primary Postgres lease and stops renewal.
/// </summary>
public sealed class PrimaryReplicaShutdownHostedService(PrimaryReplicaGate primaryGate) : IHostedService {

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task StopAsync(CancellationToken cancellationToken) =>
    await primaryGate.AbandonPrimaryAsync().ConfigureAwait(false);
}
