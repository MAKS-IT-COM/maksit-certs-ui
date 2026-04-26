using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using MaksIT.CertsUI.Engine.FluentMigrations;
using Npgsql;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// FluentMigrator runner for the Certs database: optionally creates the database, migrates up,
/// then idempotent coordination-table repair. Forward <c>Up()</c> migrations should be additive (new tables/columns); avoid dropping
/// renamed or legacy columns in <c>Up()</c> — use expand/contract and ops-driven cleanup.
/// </summary>
public sealed class RunMigrationsService(
  IMigrationRunner migrationRunner,
  ILogger<RunMigrationsService> logger,
  ICertsEngineConfiguration config
) : IRunMigrationsService {

  public async Task RunAsync(CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(config.ConnectionString))
      throw new InvalidOperationException(
        "Database connection string is empty. FluentMigrator would run in connectionless/preview mode and never commit DDL.");

    var csb = new NpgsqlConnectionStringBuilder(config.ConnectionString);
    logger.LogInformation(
      "Running Certs database migrations (host={Host}, database={Database})…",
      csb.Host ?? "(default)",
      string.IsNullOrEmpty(csb.Database) ? "(default)" : csb.Database);

    var migrationTypeCount = typeof(BaselineCertsSchema).Assembly.GetTypes()
      .Count(t => t.GetCustomAttribute<MigrationAttribute>(inherit: false) is not null);
    logger.LogInformation("FluentMigrator discovered {MigrationCount} migration type(s) in {Assembly}.", migrationTypeCount, typeof(BaselineCertsSchema).Assembly.GetName().Name);

    await EnsureDatabaseExistsAsync(cancellationToken).ConfigureAwait(false);
    await Task.Run(() => migrationRunner.MigrateUp(), cancellationToken).ConfigureAwait(false);
    await CoordinationTableProvisioner.EnsureAsync(config.ConnectionString, cancellationToken).ConfigureAwait(false);
    await VerifyCoreSchemaAsync(cancellationToken).ConfigureAwait(false);
    logger.LogInformation("Certs database migrations completed.");
  }

  /// <summary>Fails fast if the database is still empty after MigrateUp (misconfiguration, preview processor, wrong DB).</summary>
  private async Task VerifyCoreSchemaAsync(CancellationToken cancellationToken) {
    await using var conn = new NpgsqlConnection(config.ConnectionString);
    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    await using var cmd = new NpgsqlCommand(
      """
      SELECT
        EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'app_runtime_leases')
        AND (
          EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users')
          OR EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'VersionInfo')
        );
      """,
      conn);

    var any = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    if (Equals(any, true))
      return;

    throw new InvalidOperationException(
      "After migrations and coordination DDL, schema \"public\" is missing \"app_runtime_leases\" and/or core tables (\"users\" / \"VersionInfo\"). " +
      "Confirm Database= in the connection string, role CREATE privileges, and that FluentMigrator committed (non-empty connection string).");
  }

  private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken) {
    var builder = new NpgsqlConnectionStringBuilder(config.ConnectionString);
    var database = builder.Database?.Trim();
    if (string.IsNullOrEmpty(database)) return;

    builder.Database = "postgres";
    var postgresCs = builder.ConnectionString;

    try {
      await using var conn = new NpgsqlConnection(postgresCs);
      await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

      await using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbname", conn)) {
        cmd.Parameters.AddWithValue("dbname", database);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
          return;
      }

      logger.LogInformation("Database \"{Database}\" does not exist; creating it.", database);
      var quotedDb = $"\"{database.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
      await using (var createCmd = new NpgsqlCommand($"CREATE DATABASE {quotedDb}", conn)) {
        await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      }

      logger.LogInformation("Database \"{Database}\" created.", database);
    }
    catch (Exception ex) {
      logger.LogWarning(
        ex,
        "Could not use maintenance connection to database \"postgres\" for auto-create of \"{TargetDatabase}\". " +
        "If the target database already exists, migrations will continue; otherwise create the database manually.",
        database);
    }
  }
}
