using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260427203000)]
public class AcmeSessions : Migration {
  public override void Up() {
    Create.Table("acme_sessions")
      .WithColumn("session_id").AsGuid().PrimaryKey()
      .WithColumn("payload_json").AsCustom("text").NotNullable()
      .WithColumn("updated_at_utc").AsDateTimeOffset().NotNullable()
      .WithColumn("expires_at_utc").AsDateTimeOffset().NotNullable();

    Create.Index("IX_acme_sessions_expires_at_utc").OnTable("acme_sessions").OnColumn("expires_at_utc");
  }

  public override void Down() {
    Delete.Index("IX_acme_sessions_expires_at_utc").OnTable("acme_sessions");
    Delete.Table("acme_sessions");
  }
}
