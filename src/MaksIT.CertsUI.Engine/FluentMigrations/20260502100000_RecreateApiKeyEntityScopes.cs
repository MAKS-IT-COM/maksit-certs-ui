using System.Data;
using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Restores <c>api_key_entity_scopes</c> after <see cref="DropApiKeyEntityScopesTable"/> (Vault-shaped ACL rows per API key).
/// </summary>
[Migration(20260502100000)]
public class RecreateApiKeyEntityScopes : Migration {
  public override void Up() {
    if (Schema.Table("api_key_entity_scopes").Exists())
      return;

    Create.Table("api_key_entity_scopes")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("ApiKeyId").AsGuid().NotNullable()
      .WithColumn("EntityId").AsGuid().NotNullable()
      .WithColumn("EntityType").AsInt16().NotNullable()
      .WithColumn("Scope").AsInt16().NotNullable();

    Create.ForeignKey("fk_api_key_entity_scopes_api_keys")
      .FromTable("api_key_entity_scopes").ForeignColumn("ApiKeyId")
      .ToTable("api_keys").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.Index("IX_api_key_entity_scopes_ApiKeyId").OnTable("api_key_entity_scopes").OnColumn("ApiKeyId");
  }

  public override void Down() {
    Delete.Index("IX_api_key_entity_scopes_ApiKeyId").OnTable("api_key_entity_scopes");
    Delete.ForeignKey("fk_api_key_entity_scopes_api_keys").OnTable("api_key_entity_scopes");
    Delete.Table("api_key_entity_scopes");
  }
}
