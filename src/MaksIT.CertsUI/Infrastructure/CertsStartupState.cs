using System.Collections.Concurrent;
using System.Diagnostics;
using MaksIT.CertsUI.Engine.Infrastructure;

namespace MaksIT.CertsUI.Infrastructure;

public enum CertsStartupPhase {
  NotStarted = 0,
  PostgresMaintenanceReady = 10,
  DatabaseEnsured = 20,
  PostgresApplicationReady = 30,
  MigrationsComplete = 40,
  CoordinationTablesReady = 50,
  SchemaVerified = 60,
  SchemaSyncComplete = 70,
  BootstrapCoordinationComplete = 80,
}

/// <summary>
/// Tracks Certs UI startup phases and timings for health probes and operational visibility.
/// </summary>
public sealed class CertsStartupState : IDatabaseStartupObserver {
  private static readonly IReadOnlyDictionary<string, CertsStartupPhase> PhaseMap =
    new Dictionary<string, CertsStartupPhase>(StringComparer.Ordinal) {
      [DatabaseStartupPhases.PostgresMaintenanceReady] = CertsStartupPhase.PostgresMaintenanceReady,
      [DatabaseStartupPhases.DatabaseEnsured] = CertsStartupPhase.DatabaseEnsured,
      [DatabaseStartupPhases.PostgresApplicationReady] = CertsStartupPhase.PostgresApplicationReady,
      [DatabaseStartupPhases.MigrationsComplete] = CertsStartupPhase.MigrationsComplete,
      [DatabaseStartupPhases.CoordinationTablesReady] = CertsStartupPhase.CoordinationTablesReady,
      [DatabaseStartupPhases.SchemaVerified] = CertsStartupPhase.SchemaVerified,
      [DatabaseStartupPhases.SchemaSync] = CertsStartupPhase.SchemaSyncComplete,
      [CertsApplicationStartupPhases.BootstrapCoordination] = CertsStartupPhase.BootstrapCoordinationComplete,
    };

  private readonly ConcurrentDictionary<string, StartupPhaseRecord> _phases = new(StringComparer.Ordinal);
  private readonly object _gate = new();
  private readonly Stopwatch _total = Stopwatch.StartNew();
  private volatile CertsStartupPhase _current = CertsStartupPhase.NotStarted;
  private string? _lastFailedPhase;
  private string? _lastFailureMessage;

  public CertsStartupPhase CurrentPhase => _current;

  public bool IsApplicationReady => _current >= CertsStartupPhase.BootstrapCoordinationComplete;

  public bool HasFailure => _lastFailedPhase is not null;

  void IDatabaseStartupObserver.OnPhaseStarted(string phase) {
    _phases[phase] = new StartupPhaseRecord(phase, StartedAtUtc: DateTimeOffset.UtcNow);
  }

  void IDatabaseStartupObserver.OnPhaseCompleted(string phase, TimeSpan elapsed) {
    AdvancePhase(phase, elapsed, failed: false);
  }

  void IDatabaseStartupObserver.OnPhaseFailed(string phase, TimeSpan elapsed, Exception exception) {
    lock (_gate) {
      _lastFailedPhase = phase;
      _lastFailureMessage = exception.Message;
    }

    if (_phases.TryGetValue(phase, out var existing))
      _phases[phase] = existing with {
        CompletedAtUtc = DateTimeOffset.UtcNow,
        Elapsed = elapsed,
        Failed = true,
        Error = exception.Message,
      };
    else
      _phases[phase] = new StartupPhaseRecord(phase, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, elapsed, true, exception.Message);
  }

  private void AdvancePhase(string phase, TimeSpan elapsed, bool failed) {
    var completedAt = DateTimeOffset.UtcNow;
    _phases.AddOrUpdate(
      phase,
      _ => new StartupPhaseRecord(phase, completedAt, completedAt, elapsed, failed, null),
      (_, existing) => existing with {
        CompletedAtUtc = completedAt,
        Elapsed = elapsed,
        Failed = failed,
      });

    if (!failed && PhaseMap.TryGetValue(phase, out var mapped)) {
      if (mapped > _current)
        _current = mapped;

      if (mapped == CertsStartupPhase.BootstrapCoordinationComplete) {
        lock (_gate) {
          _lastFailedPhase = null;
          _lastFailureMessage = null;
        }
      }
    }
  }

  public void MarkBootstrapCoordinationComplete(TimeSpan elapsed) {
    AdvancePhase(CertsApplicationStartupPhases.BootstrapCoordination, elapsed, failed: false);
  }

  public StartupStatusSnapshot GetSnapshot() {
    var phases = _phases.Values
      .OrderBy(p => p.StartedAtUtc ?? DateTimeOffset.MaxValue)
      .ThenBy(p => p.Name, StringComparer.Ordinal)
      .ToList();

    lock (_gate) {
      return new StartupStatusSnapshot(
        IsApplicationReady,
        _current.ToString(),
        _total.Elapsed,
        _lastFailedPhase,
        _lastFailureMessage,
        phases);
    }
  }
}

public static class CertsApplicationStartupPhases {
  public const string BootstrapCoordination = "bootstrap_coordination";
}

public sealed record StartupPhaseRecord(
  string Name,
  DateTimeOffset? StartedAtUtc = null,
  DateTimeOffset? CompletedAtUtc = null,
  TimeSpan? Elapsed = null,
  bool Failed = false,
  string? Error = null
);

public sealed record StartupStatusSnapshot(
  bool IsApplicationReady,
  string CurrentPhase,
  TimeSpan TotalElapsed,
  string? LastFailedPhase,
  string? LastFailureMessage,
  IReadOnlyList<StartupPhaseRecord> Phases
);
