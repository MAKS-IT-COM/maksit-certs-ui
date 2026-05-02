using Npgsql;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Idempotent DDL for HA coordination tables in schema <c>public</c> (same shape as the AcmeChallengesAndRuntimeLeases migration). Used after FluentMigrator and again before bootstrap lease
/// so <see cref="RuntimeLeaseServiceNpgsql"/> never runs against a missing <c>app_runtime_leases</c>.
/// </summary>
public static class CoordinationTableProvisioner {

  /// <summary>Creates <c>public.acme_http_challenges</c>, <c>public.app_runtime_leases</c>, and <c>public.acme_sessions</c> if missing.</summary>
  public static async Task EnsureAsync(string? connectionString, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(connectionString))
      return;

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    await using var cmd = new NpgsqlCommand(
      """
      CREATE TABLE IF NOT EXISTS public.acme_http_challenges (
        file_name text NOT NULL PRIMARY KEY,
        token_value text NOT NULL,
        created_at_utc timestamp with time zone NOT NULL
      );
      CREATE INDEX IF NOT EXISTS "IX_acme_http_challenges_created_at_utc" ON public.acme_http_challenges (created_at_utc);
      CREATE TABLE IF NOT EXISTS public.app_runtime_leases (
        lease_name text NOT NULL PRIMARY KEY,
        holder_id text NOT NULL,
        version bigint NOT NULL DEFAULT 1,
        acquired_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL
      );
      CREATE TABLE IF NOT EXISTS public.acme_sessions (
        session_id uuid NOT NULL PRIMARY KEY,
        payload_json text NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL
      );
      CREATE INDEX IF NOT EXISTS "IX_acme_sessions_expires_at_utc" ON public.acme_sessions (expires_at_utc);
      ALTER TABLE public.acme_sessions ADD COLUMN IF NOT EXISTS account_scope_id uuid NULL;
      CREATE INDEX IF NOT EXISTS "IX_acme_sessions_account_scope_id" ON public.acme_sessions (account_scope_id);
      """,
      conn);
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
  }
}
