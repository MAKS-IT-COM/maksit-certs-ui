using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Previously re-added <c>users.JwtTokensJson</c> when a prior migration had dropped that JSON column.
/// <see cref="DropUsersJwtTokensJson"/> removes the column; persisted sessions use <c>jwt_tokens</c> only.
/// </summary>
[Migration(20260426120000)]
public class RestoreUsersJwtTokensJsonIfDropped : Migration {
  public override void Up() {
    // No-op: revision kept so databases that already applied the old DDL remain valid; see DropUsersJwtTokensJson.
  }

  public override void Down() =>
    throw new NotSupportedException("Down is not supported for this repair migration.");
}
