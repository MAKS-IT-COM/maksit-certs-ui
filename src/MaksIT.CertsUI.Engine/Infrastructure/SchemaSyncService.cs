using Microsoft.Extensions.Logging;
using Npgsql;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Syncs the database schema to match DTOs: add missing tables and columns only (no DROP).
/// Runs after FluentMigrator so baseline tables exist; this adds any missing columns (and optionally missing tables).
/// </summary>
public class SchemaSyncService(ICertsEngineConfiguration config, ILogger<SchemaSyncService> logger) : ISchemaSyncService {
  private readonly ICertsEngineConfiguration _config = config;
  private readonly ILogger<SchemaSyncService> _logger = logger;

  public async Task SyncSchemaAsync(CancellationToken cancellationToken = default) {
    if (!_config.AutoSyncSchema)
      return;

    _logger.LogInformation("Schema sync (add-only) starting...");

    var desired = GetDesiredSchema();

    var current = await GetCurrentSchemaAsync(cancellationToken);

    var ddl = BuildAddOnlyDdl(desired, current);
    if (ddl.Count == 0) {
      _logger.LogInformation("Schema sync: no changes needed.");
      return;
    }

    await using var conn = new NpgsqlConnection(_config.ConnectionString);

    await conn.OpenAsync(cancellationToken);

    await using var tx = await conn.BeginTransactionAsync(cancellationToken);

    try {
      foreach (var sql in ddl) {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Executed: {Sql}", sql);
      }

      await tx.CommitAsync(cancellationToken);
      _logger.LogInformation("Schema sync completed. Applied {Count} DDL statement(s).", ddl.Count);
    }
    catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }
  }

  /// <summary>Desired schema: table → list of (column name, pg type). Matches FluentMigrator baseline (lowercase/snake_case).</summary>
  private static Dictionary<string, List<(string Column, string PgType)>> GetDesiredSchema() {
    return new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase) {
      ["registration_caches"] = [
        ("AccountId", "uuid"),
        ("Version", "bigint"),
        ("PayloadJson", "text"),
      ],
      ["acme_http_challenges"] = [
        ("file_name", "text"),
        ("token_value", "text"),
        ("created_at_utc", "timestamp with time zone"),
      ],
      ["app_runtime_leases"] = [
        ("lease_name", "text"),
        ("holder_id", "text"),
        ("version", "bigint"),
        ("acquired_at_utc", "timestamp with time zone"),
        ("expires_at_utc", "timestamp with time zone"),
      ],
      ["acme_sessions"] = [
        ("session_id", "uuid"),
        ("payload_json", "text"),
        ("updated_at_utc", "timestamp with time zone"),
        ("expires_at_utc", "timestamp with time zone"),
        ("account_scope_id", "uuid"),
      ],
      ["api_keys"] = [
        ("id", "uuid"),
        ("description", "text"),
        ("created_at", "timestamp with time zone"),
        ("revoked_at", "timestamp with time zone"),
        ("expires_at", "timestamp with time zone"),
        ("api_key", "text"),
        ("is_global_admin", "boolean"),
      ],
      ["users"] = [
        ("id", "uuid"),
        ("username", "text"),
        ("email", "text"),
        ("mobile_number", "text"),
        ("is_active", "boolean"),
        ("is_global_admin", "boolean"),
        ("password_salt", "text"),
        ("password_hash", "text"),
        ("two_factor_shared_key", "text"),
        ("created_at", "timestamp with time zone"),
        ("last_login", "timestamp with time zone"),
      ],
      ["jwt_tokens"] = [
        ("id", "uuid"),
        ("user_id", "uuid"),
        ("token", "text"),
        ("refresh_token", "text"),
        ("issued_at", "timestamp with time zone"),
        ("expires_at", "timestamp with time zone"),
        ("refresh_token_expires_at", "timestamp with time zone"),
        ("is_revoked", "boolean"),
      ],
      ["two_factor_recovery_codes"] = [
        ("id", "uuid"),
        ("user_id", "uuid"),
        ("salt", "text"),
        ("hash", "text"),
        ("is_used", "boolean"),
      ],
      ["user_entity_scopes"] = [
        ("id", "uuid"),
        ("user_id", "uuid"),
        ("entity_id", "uuid"),
        ("entity_type", "smallint"),
        ("scope", "smallint"),
      ],
      ["api_key_entity_scopes"] = [
        ("id", "uuid"),
        ("api_key_id", "uuid"),
        ("entity_id", "uuid"),
        ("entity_type", "smallint"),
        ("scope", "smallint"),
      ],
    };
  }

  private async Task<Dictionary<string, HashSet<string>>> GetCurrentSchemaAsync(CancellationToken ct) {
    var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    await using var conn = new NpgsqlConnection(_config.ConnectionString);

    await conn.OpenAsync(ct);

    await using var cmd = new NpgsqlCommand("""
      SELECT table_name, column_name
      FROM information_schema.columns
      WHERE table_schema = 'public'
      ORDER BY table_name, ordinal_position
      """, conn);

    await using var reader = await cmd.ExecuteReaderAsync(ct);

    while (await reader.ReadAsync(ct)) {
      var table = reader.GetString(0);
      var column = reader.GetString(1);

      if (!result.TryGetValue(table, out var columns))
        result[table] = columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      columns.Add(column);
    }

    return result;
  }

  /// <summary>Returns DDL list: only ALTER TABLE ADD COLUMN for missing columns. No CREATE TABLE (FluentMigrator handles tables). No DROP.</summary>
  private static List<string> BuildAddOnlyDdl(
    Dictionary<string, List<(string Column, string PgType)>> desired,
    Dictionary<string, HashSet<string>> current) {
    List<string> ddl = [];

    foreach (var (table, columns) in desired) {
      if (!current.TryGetValue(table, out var existingColumns))
        continue;

      foreach (var (column, pgType) in columns) {
        if (existingColumns.Contains(column))
          continue;

        if (table.Equals("registration_caches", StringComparison.OrdinalIgnoreCase)
            && column.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
          ddl.Add($"ALTER TABLE {Quote(table)} ADD COLUMN IF NOT EXISTS {Quote(column)} bigint NOT NULL DEFAULT 1");
          continue;
        }

        var nullable = (pgType == "text"
          || column == "expires_at"
          || column == "last_login"
          || column == "description"
          || column == "two_factor_shared_key"
          || column == "email"
          || column == "mobile_number"
          || column == "revoked_at"
          || column == "api_key"
          || column == "account_scope_id"
          || column == "payload_json") ? "" : " NOT NULL";

        ddl.Add($"ALTER TABLE {Quote(table)} ADD COLUMN IF NOT EXISTS {Quote(column)} {pgType}{nullable}");
      }
    }

    return ddl;
  }

  private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
