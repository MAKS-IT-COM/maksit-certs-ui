using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260430100000)]
public class AcmeSessionsAccountScope : Migration {
  public override void Up() {
    Alter.Table("acme_sessions")
      .AddColumn("account_scope_id").AsGuid().Nullable();

    Create.Index("IX_acme_sessions_account_scope_id")
      .OnTable("acme_sessions")
      .OnColumn("account_scope_id");
  }

  public override void Down() {
    Delete.Index("IX_acme_sessions_account_scope_id").OnTable("acme_sessions");
    Delete.Column("account_scope_id").FromTable("acme_sessions");
  }
}
