using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(Version)]
public class BaselineCertsSchema : Migration {
  public const long Version = 20260415100000L;
  public override void Up() {
    Create.Table("registration_caches")
      .WithColumn("AccountId").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("PayloadJson").AsCustom("text").NotNullable();

    Create.Table("api_keys")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("Description").AsCustom("text").Nullable()
      .WithColumn("KeyHashHex").AsCustom("text").NotNullable()
      .WithColumn("CreatedAtUtc").AsDateTimeOffset().NotNullable()
      .WithColumn("RevokedAtUtc").AsDateTimeOffset().Nullable()
      .WithColumn("ExpiresAtUtc").AsDateTimeOffset().Nullable();

    Create.Index("IX_api_keys_KeyHashHex").OnTable("api_keys").OnColumn("KeyHashHex");

    Create.Table("users")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("Name").AsCustom("text").NotNullable()
      .WithColumn("Salt").AsCustom("text").NotNullable()
      .WithColumn("Hash").AsCustom("text").NotNullable()
      .WithColumn("JwtTokensJson").AsCustom("text").NotNullable()
      .WithColumn("LastLoginUtc").AsDateTimeOffset().NotNullable();

    Create.Index("IX_users_Name").OnTable("users").OnColumn("Name").Unique();
  }

  public override void Down() {
    Delete.Index("IX_users_Name").OnTable("users");
    Delete.Table("users");
    Delete.Index("IX_api_keys_KeyHashHex").OnTable("api_keys");
    Delete.Table("api_keys");
    Delete.Table("registration_caches");
  }
}
