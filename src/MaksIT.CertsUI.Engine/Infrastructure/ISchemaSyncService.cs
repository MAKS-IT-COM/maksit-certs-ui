namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Syncs the database schema to match DTOs: add missing tables and columns only (no DROP).
/// Called from startup when <see cref="ICertsEngineConfiguration.AutoSyncSchema"/> is true.
/// </summary>
public interface ISchemaSyncService {
  Task SyncSchemaAsync(CancellationToken cancellationToken = default);
}
