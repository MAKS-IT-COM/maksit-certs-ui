using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using MaksIT.CertsUI.Engine.FluentMigrations;
using Npgsql;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// FluentMigrator runner for the Certs database: optionally creates the database, baselines legacy EF-created schemas, then migrates up.
/// </summary>
public sealed class RunMigrationsService(
  IMigrationRunner migrationRunner,
  ILogger<RunMigrationsService> logger,
  ICertsEngineConfiguration config
) : IRunMigrationsService {

  public static long BaselineVersion => BaselineCertsSchema.Version;

  public async Task RunAsync(CancellationToken cancellationToken = default) {
    logger.LogInformation("Running Certs database migrations...");
    await EnsureDatabaseExistsAsync(cancellationToken).ConfigureAwait(false);
    await BaselineExistingEfDatabaseAsync(cancellationToken).ConfigureAwait(false);
    await Task.Run(() => migrationRunner.MigrateUp(), cancellationToken).ConfigureAwait(false);
    logger.LogInformation("Certs database migrations completed.");
  }

  private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken) {
    var builder = new NpgsqlConnectionStringBuilder(config.ConnectionString);
    var database = builder.Database?.Trim();
    if (string.IsNullOrEmpty(database)) return;

    builder.Database = "postgres";
    var postgresCs = builder.ConnectionString;

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

  /// <summary>
  /// If the database already has Certs tables from legacy EF Core migrations, mark the FluentMigrator baseline as applied.
  /// </summary>
  private async Task BaselineExistingEfDatabaseAsync(CancellationToken cancellationToken) {
    await using var conn = new NpgsqlConnection(config.ConnectionString);
    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    await using (var cmd = new NpgsqlCommand(
      "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users' LIMIT 1",
      conn)) {
      await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
      if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        return;
    }

    logger.LogInformation("Existing Certs schema detected; baselining FluentMigrator VersionInfo if needed.");

    await using (var cmd = new NpgsqlCommand(@"
      CREATE TABLE IF NOT EXISTS ""VersionInfo"" (
        ""Version"" bigint NOT NULL PRIMARY KEY,
        ""AppliedOn"" timestamp NULL,
        ""Description"" varchar(1024) NULL
      )", conn)) {
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (var cmd = new NpgsqlCommand(
      @"INSERT INTO ""VersionInfo"" (""Version"", ""AppliedOn"", ""Description"")
        VALUES (@v, @appliedOn, @desc)
        ON CONFLICT (""Version"") DO NOTHING",
      conn)) {
      cmd.Parameters.AddWithValue("v", BaselineCertsSchema.Version);
      cmd.Parameters.AddWithValue("appliedOn", DBNull.Value);
      cmd.Parameters.AddWithValue("desc", "BaselineCertsSchema (existing DB from EF Core)");
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
  }
}
