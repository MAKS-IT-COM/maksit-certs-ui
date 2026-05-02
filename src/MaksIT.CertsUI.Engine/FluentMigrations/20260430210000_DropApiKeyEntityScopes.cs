using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>Drops legacy <c>api_key_entity_scopes</c> table if present.</summary>
[Migration(20260430210000)]
public class DropApiKeyEntityScopesTable : Migration {
  public override void Up() {
    Execute.Sql("DROP TABLE IF EXISTS public.api_key_entity_scopes CASCADE;");
  }

  public override void Down() {
  }
}
