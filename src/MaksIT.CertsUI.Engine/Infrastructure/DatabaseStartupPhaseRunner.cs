using System.Diagnostics;

namespace MaksIT.CertsUI.Engine.Infrastructure;

internal static class DatabaseStartupPhaseRunner {
  public static async Task RunAsync(
    IDatabaseStartupObserver observer,
    string phase,
    Func<CancellationToken, Task> action,
    CancellationToken cancellationToken
  ) {
    observer.OnPhaseStarted(phase);
    var sw = Stopwatch.StartNew();
    try {
      await action(cancellationToken).ConfigureAwait(false);
      observer.OnPhaseCompleted(phase, sw.Elapsed);
    }
    catch (Exception ex) {
      observer.OnPhaseFailed(phase, sw.Elapsed, ex);
      throw;
    }
  }
}
