using System.Data;
using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260420100000)]
public class UserIsActiveAndTwoFactor : Migration {
  public override void Up() {
    Alter.Table("users")
      .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true);

    Alter.Table("users")
      .AddColumn("TwoFactorSharedKey").AsCustom("text").Nullable();

    Create.Table("two_factor_recovery_codes")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("UserId").AsGuid().NotNullable()
      .WithColumn("Salt").AsCustom("text").NotNullable()
      .WithColumn("Hash").AsCustom("text").NotNullable()
      .WithColumn("IsUsed").AsBoolean().NotNullable().WithDefaultValue(false);

    Create.ForeignKey("FK_two_factor_recovery_codes_users")
      .FromTable("two_factor_recovery_codes").ForeignColumn("UserId")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.Index("IX_two_factor_recovery_codes_UserId").OnTable("two_factor_recovery_codes").OnColumn("UserId");
  }

  public override void Down() {
    Delete.Index("IX_two_factor_recovery_codes_UserId").OnTable("two_factor_recovery_codes");
    Delete.ForeignKey("FK_two_factor_recovery_codes_users").OnTable("two_factor_recovery_codes");
    Delete.Table("two_factor_recovery_codes");

    Delete.Column("TwoFactorSharedKey").FromTable("users");
    Delete.Column("IsActive").FromTable("users");
  }
}
