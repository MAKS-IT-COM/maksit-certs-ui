namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Syncs the database schema toward DTOs: <c>ADD COLUMN IF NOT EXISTS</c> only (no DROP of legacy or renamed columns).
/// Called from startup when <see cref="ICertsEngineConfiguration.AutoSyncSchema"/> is true (recommended).
/// </summary>
public interface ISchemaSyncService {
  Task SyncSchemaAsync(CancellationToken cancellationToken = default);
}
