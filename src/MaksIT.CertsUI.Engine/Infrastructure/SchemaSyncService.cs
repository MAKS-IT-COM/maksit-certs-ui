using Microsoft.Extensions.Logging;
using Npgsql;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Add-only schema sync: adds missing columns to match DTOs (no DROP). Runs after FluentMigrator; table creation stays in migrations.
/// </summary>
public class SchemaSyncService(ICertsEngineConfiguration config, ILogger<SchemaSyncService> logger) : ISchemaSyncService {
  private readonly ICertsEngineConfiguration _config = config;
  private readonly ILogger<SchemaSyncService> _logger = logger;

  public async Task SyncSchemaAsync(CancellationToken cancellationToken = default) {
    if (!_config.AutoSyncSchema)
      return;

    _logger.LogInformation("Schema sync (add-only) starting...");

    var desired = GetDesiredSchema();

    var current = await GetCurrentSchemaAsync(cancellationToken).ConfigureAwait(false);

    var ddl = BuildAddOnlyDdl(desired, current);
    if (ddl.Count == 0) {
      _logger.LogInformation("Schema sync: no changes needed.");
      return;
    }

    await using var conn = new NpgsqlConnection(_config.ConnectionString);

    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    try {
      foreach (var sql in ddl) {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Executed: {Sql}", sql);
      }

      await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
      _logger.LogInformation("Schema sync completed. Applied {Count} DDL statement(s).", ddl.Count);
    }
    catch {
      await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
      throw;
    }
  }

  /// <summary>Desired schema: table → (column name, pg type). Matches FluentMigrator baseline + jwt_tokens migration (lowercase/snake table names).</summary>
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
      ["api_keys"] = [
        ("Id", "uuid"),
        ("Description", "text"),
        ("KeySalt", "text"),
        ("KeyHashHex", "text"),
        ("CreatedAtUtc", "timestamp with time zone"),
        ("RevokedAtUtc", "timestamp with time zone"),
        ("ExpiresAtUtc", "timestamp with time zone"),
      ],
      ["users"] = [
        ("Id", "uuid"),
        ("Name", "text"),
        ("Salt", "text"),
        ("Hash", "text"),
        ("LastLoginUtc", "timestamp with time zone"),
      ],
      ["jwt_tokens"] = [
        ("Id", "uuid"),
        ("UserId", "uuid"),
        ("Token", "text"),
        ("RefreshToken", "text"),
        ("IssuedAt", "timestamp with time zone"),
        ("ExpiresAt", "timestamp with time zone"),
        ("RefreshTokenExpiresAt", "timestamp with time zone"),
        ("IsRevoked", "boolean"),
      ],
    };
  }

  private async Task<Dictionary<string, HashSet<string>>> GetCurrentSchemaAsync(CancellationToken ct) {
    var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    await using var conn = new NpgsqlConnection(_config.ConnectionString);

    await conn.OpenAsync(ct).ConfigureAwait(false);

    await using var cmd = new NpgsqlCommand("""
      SELECT table_name, column_name
      FROM information_schema.columns
      WHERE table_schema = 'public'
      ORDER BY table_name, ordinal_position
      """, conn);

    await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

    while (await reader.ReadAsync(ct).ConfigureAwait(false)) {
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

        var notNullSuffix = ShouldAddNotNull(table, column) ? " NOT NULL" : "";
        ddl.Add($"ALTER TABLE {Quote(table)} ADD COLUMN IF NOT EXISTS {Quote(column)} {pgType}{notNullSuffix}");
      }
    }

    return ddl;
  }

  /// <summary>Nullable columns only (matches FluentMigrator nullability for optional fields).</summary>
  private static bool ShouldAddNotNull(string table, string column) {
    if (table.Equals("api_keys", StringComparison.OrdinalIgnoreCase)) {
      if (column.Equals("Description", StringComparison.OrdinalIgnoreCase)) return false;
      if (column.Equals("RevokedAtUtc", StringComparison.OrdinalIgnoreCase)) return false;
      if (column.Equals("ExpiresAtUtc", StringComparison.OrdinalIgnoreCase)) return false;
    }

    return true;
  }

  private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

}
