using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Databases that already applied <see cref="JwtTokensTableMigrateFromJson"/> when it still dropped <c>JwtTokensJson</c>
/// get the column back (empty default). Expand-only: we never remove renamed/legacy columns in <c>Up()</c>.
/// </summary>
[Migration(20260426120000)]
public class RestoreUsersJwtTokensJsonIfDropped : Migration {
  public override void Up() {
    Execute.Sql("""
      ALTER TABLE "users" ADD COLUMN IF NOT EXISTS "JwtTokensJson" text NOT NULL DEFAULT '';
      """);
  }

  public override void Down() =>
    throw new NotSupportedException("Down is not supported for this repair migration.");
}
