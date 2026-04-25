using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260425100000)]
public class RegistrationCacheVersionColumn : Migration {
  public override void Up() {
    Alter.Table("registration_caches")
      .AddColumn("Version").AsInt64().NotNullable().WithDefaultValue(1);
  }

  public override void Down() {
    Delete.Column("Version").FromTable("registration_caches");
  }
}
