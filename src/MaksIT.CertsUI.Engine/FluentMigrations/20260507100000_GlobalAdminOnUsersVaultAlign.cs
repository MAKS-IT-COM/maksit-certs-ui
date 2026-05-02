using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Aligns with Vault baseline: <c>users.is_global_admin</c> holds the flag.
/// Migrates data from legacy <c>user_authorizations</c> then drops that table so <c>users</c> matches Vault-style DDL (<c>users.is_global_admin</c>).
/// </summary>
[Migration(20260507100000)]
public class GlobalAdminOnUsersVaultAlign : Migration {

  public override void Up() {
    Execute.Sql("""
      ALTER TABLE public.users ADD COLUMN IF NOT EXISTS is_global_admin boolean NOT NULL DEFAULT false;

      DO $$
      BEGIN
        IF EXISTS (
          SELECT 1 FROM information_schema.tables
          WHERE table_schema = 'public' AND table_name = 'user_authorizations'
        ) THEN
          UPDATE public.users u
          SET is_global_admin = ua.is_global_admin
          FROM public.user_authorizations ua
          WHERE u.id = ua.user_id;
          DROP TABLE public.user_authorizations CASCADE;
        END IF;
      END $$;
      """);
  }

  public override void Down() {
    // Forward-only per expand-only migration policy.
  }
}
