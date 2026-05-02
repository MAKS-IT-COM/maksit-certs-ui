using System.Data;
using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>Adds user RBAC tables (Vault-shaped; Certs-specific scope enums in application layer).</summary>
[Migration(20260501190000)]
public class UserAuthorizationsAndEntityScopes : Migration {
  public override void Up() {
    Create.Table("user_authorizations")
      .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("IsGlobalAdmin").AsBoolean().NotNullable().WithDefaultValue(false);

    Create.ForeignKey("fk_user_authorizations_users")
      .FromTable("user_authorizations").ForeignColumn("UserId")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.Table("user_entity_scopes")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("UserId").AsGuid().NotNullable()
      .WithColumn("EntityId").AsGuid().NotNullable()
      .WithColumn("EntityType").AsInt16().NotNullable()
      .WithColumn("Scope").AsInt16().NotNullable();

    Create.ForeignKey("fk_user_entity_scopes_users")
      .FromTable("user_entity_scopes").ForeignColumn("UserId")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.Index("IX_user_entity_scopes_UserId").OnTable("user_entity_scopes").OnColumn("UserId");
  }

  public override void Down() {
    Delete.ForeignKey("fk_user_entity_scopes_users").OnTable("user_entity_scopes");
    Delete.Index("IX_user_entity_scopes_UserId").OnTable("user_entity_scopes");
    Delete.Table("user_entity_scopes");

    Delete.ForeignKey("fk_user_authorizations_users").OnTable("user_authorizations");
    Delete.Table("user_authorizations");
  }
}
