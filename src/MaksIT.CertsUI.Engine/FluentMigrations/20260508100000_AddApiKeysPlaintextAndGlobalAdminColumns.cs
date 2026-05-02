using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Ensures <c>api_keys.api_key</c> (plaintext secret material) and <c>is_global_admin</c> exist.
/// Previously these could only appear via <see cref="SchemaSyncService"/>; FluentMigrator must own DDL so DBs always match <see cref="Data.CertsUILinq2DbMapping"/>.
/// </summary>
[Migration(20260508100000)]
public class AddApiKeysPlaintextAndGlobalAdminColumns : Migration {

  public override void Up() {
    Execute.Sql("""
      ALTER TABLE public.api_keys ADD COLUMN IF NOT EXISTS api_key text;
      ALTER TABLE public.api_keys ADD COLUMN IF NOT EXISTS is_global_admin boolean NOT NULL DEFAULT false;
      """);
  }

  public override void Down() {
    // Forward-only: dropping columns risks data loss.
  }
}
