using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260425130000)]
public class AcmeChallengesAndRuntimeLeases : Migration {
  public override void Up() {
    Create.Table("acme_http_challenges")
      .WithColumn("file_name").AsCustom("text").NotNullable().PrimaryKey()
      .WithColumn("token_value").AsCustom("text").NotNullable()
      .WithColumn("created_at_utc").AsDateTimeOffset().NotNullable();

    Create.Index("IX_acme_http_challenges_created_at_utc").OnTable("acme_http_challenges").OnColumn("created_at_utc");

    Create.Table("app_runtime_leases")
      .WithColumn("lease_name").AsCustom("text").NotNullable().PrimaryKey()
      .WithColumn("holder_id").AsCustom("text").NotNullable()
      .WithColumn("version").AsInt64().NotNullable().WithDefaultValue(1)
      .WithColumn("acquired_at_utc").AsDateTimeOffset().NotNullable()
      .WithColumn("expires_at_utc").AsDateTimeOffset().NotNullable();
  }

  public override void Down() {
    Delete.Index("IX_acme_http_challenges_created_at_utc").OnTable("acme_http_challenges");
    Delete.Table("acme_http_challenges");
    Delete.Table("app_runtime_leases");
  }
}
