using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Drops <c>users.JwtTokensJson</c> when present (old JSON copy of sessions). Sessions remain in <c>jwt_tokens</c> for server-side validation / allowlist behavior.
/// </summary>
[Migration(20260426140000)]
public class DropUsersJwtTokensJson : Migration {
  public override void Up() {
    if (Schema.Table("users").Column("JwtTokensJson").Exists())
      Delete.Column("JwtTokensJson").FromTable("users");
  }

  public override void Down() =>
    throw new NotSupportedException("Down is not supported; restore from backup if required.");
}
