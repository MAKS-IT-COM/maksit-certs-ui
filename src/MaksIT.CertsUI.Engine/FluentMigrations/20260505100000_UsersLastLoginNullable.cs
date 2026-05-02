using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Aligns <c>users.last_login</c> with <see cref="MaksIT.CertsUI.Engine.Dto.Identity.UserDto.LastLogin"/> (<c>DateTime?</c>): no default at insert before first login.
/// </summary>
[Migration(20260505100000)]
public class UsersLastLoginNullable : Migration {

  public override void Up() {
    Execute.Sql("""
      DO $$
      BEGIN
        IF EXISTS (
          SELECT 1 FROM pg_attribute a
          INNER JOIN pg_class c ON c.oid = a.attrelid
          INNER JOIN pg_namespace n ON n.oid = c.relnamespace
          WHERE n.nspname = 'public'
            AND c.relname = 'users'
            AND a.attname = 'last_login'
            AND a.attnum > 0
            AND NOT a.attisdropped
            AND a.attnotnull
        ) THEN
          EXECUTE 'ALTER TABLE public.users ALTER COLUMN last_login DROP NOT NULL';
        END IF;
        IF EXISTS (
          SELECT 1 FROM pg_attribute a
          INNER JOIN pg_class c ON c.oid = a.attrelid
          INNER JOIN pg_namespace n ON n.oid = c.relnamespace
          WHERE n.nspname = 'public'
            AND c.relname = 'users'
            AND a.attname = 'LastLoginUtc'
            AND a.attnum > 0
            AND NOT a.attisdropped
            AND a.attnotnull
        ) THEN
          EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I DROP NOT NULL', 'public', 'users', 'LastLoginUtc');
        END IF;
      END $$;
      """);
  }

  public override void Down() {
    // Forward-only per expand-only migration policy.
  }
}
