namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Optional hooks for reporting database startup phase timing (migrations, schema sync).
/// </summary>
public interface IDatabaseStartupObserver {
  void OnPhaseStarted(string phase);
  void OnPhaseCompleted(string phase, TimeSpan elapsed);
  void OnPhaseFailed(string phase, TimeSpan elapsed, Exception exception);
}

/// <summary>Well-known phase names reported during <see cref="IRunMigrationsService.RunAsync"/> and schema sync.</summary>
public static class DatabaseStartupPhases {
  public const string PostgresMaintenanceReady = "postgres_maintenance_ready";
  public const string DatabaseEnsured = "database_ensured";
  public const string PostgresApplicationReady = "postgres_application_ready";
  public const string MigrationsComplete = "migrations_complete";
  public const string CoordinationTablesReady = "coordination_tables_ready";
  public const string SchemaVerified = "schema_verified";
  public const string SchemaSync = "schema_sync";
}

internal sealed class NoOpDatabaseStartupObserver : IDatabaseStartupObserver {
  public void OnPhaseStarted(string phase) { }
  public void OnPhaseCompleted(string phase, TimeSpan elapsed) { }
  public void OnPhaseFailed(string phase, TimeSpan elapsed, Exception exception) { }
}
