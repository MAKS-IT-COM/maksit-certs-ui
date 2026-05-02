using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Adds contact and audit columns expected by <see cref="MaksIT.CertsUI.Engine.Data.CertsUILinq2DbMapping"/> on <c>users</c>.
/// Baseline schema only had Id/Name/Salt/Hash/LastLoginUtc.
/// </summary>
[Migration(20260506100000)]
public class UsersContactAndCreatedAt : Migration {

  public override void Up() {
    Execute.Sql("""
      ALTER TABLE public.users ADD COLUMN IF NOT EXISTS email text;
      ALTER TABLE public.users ADD COLUMN IF NOT EXISTS mobile_number text;
      ALTER TABLE public.users ADD COLUMN IF NOT EXISTS created_at timestamp with time zone;

      UPDATE public.users
      SET created_at = COALESCE(last_login, now() AT TIME ZONE 'utc')
      WHERE created_at IS NULL;

      ALTER TABLE public.users ALTER COLUMN created_at SET NOT NULL;
      """);
  }

  public override void Down() {
    // Forward-only per expand-only migration policy.
  }
}
