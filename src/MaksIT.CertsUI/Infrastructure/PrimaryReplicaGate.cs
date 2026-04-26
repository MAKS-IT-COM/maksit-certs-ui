using Microsoft.Extensions.Hosting;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.RuntimeCoordination;

namespace MaksIT.CertsUI.Infrastructure;

/// <summary>
/// Holds <see cref="RuntimeLeaseNames.PrimaryReplica"/> and renews it while this instance is leader.
/// <see cref="IPrimaryReplicaWorkload.IsPrimary"/> stays false until <see cref="EnablePrimaryWorkload"/> runs after successful startup bootstrap.
/// </summary>
public sealed class PrimaryReplicaGate(
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance,
  ILogger<PrimaryReplicaGate> logger
) : IPrimaryReplicaWorkload, IAsyncDisposable {

  private static readonly TimeSpan PrimaryLeaseTtl = TimeSpan.FromSeconds(90);
  private static readonly TimeSpan RenewInterval = TimeSpan.FromSeconds(30);

  private readonly object _sync = new();
  private CancellationTokenSource? _renewCts;
  private Task? _renewalTask;
  private string? _holderId;
  private volatile bool _mayRunPrimaryWorkload;

  public bool IsPrimary => _mayRunPrimaryWorkload;

  /// <summary>Single attempt to insert/update the primary lease row for this holder.</summary>
  public async Task<bool> TryAcquirePrimaryLeaseAsync(CancellationToken cancellationToken) {
    var holder = runtimeInstance.InstanceId;
    var acquired = await leaseService.TryAcquireAsync(RuntimeLeaseNames.PrimaryReplica, holder, PrimaryLeaseTtl, cancellationToken).ConfigureAwait(false);
    if (!acquired.IsSuccess)
      throw new InvalidOperationException(string.Join(", ", acquired.Messages ?? ["Primary lease acquire failed."]));
    if (!acquired.Value)
      return false;

    lock (_sync) {
      _holderId = holder;
      _mayRunPrimaryWorkload = false;
    }

    return true;
  }

  /// <summary>After <see cref="TryAcquirePrimaryLeaseAsync"/> returned true, start renewal (call before long init).</summary>
  public void StartLeaseRenewal(IHostApplicationLifetime applicationLifetime) {
    lock (_sync) {
      if (_holderId == null)
        throw new InvalidOperationException("Cannot start renewal without an acquired primary lease.");
      _renewCts?.Cancel();
      _renewCts?.Dispose();
      _renewCts = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
      var holder = _holderId;
      var ct = _renewCts.Token;
      _renewalTask = RenewalLoopAsync(holder, ct);
    }
  }

  public void EnablePrimaryWorkload() => _mayRunPrimaryWorkload = true;

  private async Task RenewalLoopAsync(string holderId, CancellationToken cancellationToken) {
    try {
      while (!cancellationToken.IsCancellationRequested) {
        var renewed = await leaseService.TryAcquireAsync(RuntimeLeaseNames.PrimaryReplica, holderId, PrimaryLeaseTtl, cancellationToken).ConfigureAwait(false);
        if (!renewed.IsSuccess || !renewed.Value) {
          if (logger.IsEnabled(LogLevel.Warning))
            logger.LogWarning("Primary replica lease was not renewed (success={Success}, acquired={Acquired}).", renewed.IsSuccess, renewed.Value);
          _mayRunPrimaryWorkload = false;
          return;
        }

        await Task.Delay(RenewInterval, cancellationToken).ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
      // normal shutdown
    }
    catch (Exception ex) {
      if (logger.IsEnabled(LogLevel.Error))
        logger.LogError(ex, "Primary replica lease renewal loop failed.");
      _mayRunPrimaryWorkload = false;
    }
  }

  /// <summary>Release lease and stop renewal after failed leader bootstrap (instance stays usable for retry).</summary>
  public async Task AbandonPrimaryAsync() {
    _mayRunPrimaryWorkload = false;
    Task? renewalToAwait;
    CancellationTokenSource? cts;
    string? holder;
    lock (_sync) {
      holder = _holderId;
      _holderId = null;
      cts = _renewCts;
      _renewCts = null;
      renewalToAwait = _renewalTask;
      _renewalTask = null;
    }

    try {
      cts?.Cancel();
      if (renewalToAwait != null)
        await renewalToAwait.ConfigureAwait(false);
    }
    catch (Exception ex) {
      if (logger.IsEnabled(LogLevel.Debug))
        logger.LogDebug(ex, "Primary renewal task did not end cleanly during abandon.");
    }
    finally {
      cts?.Dispose();
    }

    if (holder != null) {
      var released = await leaseService.ReleaseAsync(RuntimeLeaseNames.PrimaryReplica, holder, CancellationToken.None).ConfigureAwait(false);
      if (!released.IsSuccess && logger.IsEnabled(LogLevel.Warning))
        logger.LogWarning("Primary lease release (abandon): {Messages}", string.Join("; ", released.Messages ?? []));
    }
  }

  public async ValueTask DisposeAsync() => await AbandonPrimaryAsync().ConfigureAwait(false);
}
