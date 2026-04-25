using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Per-key salt for API key material (pepper is app-wide from <see cref="DomainServices.IIdentityDomainConfiguration.Pepper"/>). Empty <c>KeySalt</c> denotes legacy SHA-256-only rows.
/// </summary>
[Migration(20260419100000)]
public class AddApiKeyKeySalt : Migration {
  public override void Up() {
    Alter.Table("api_keys")
      .AddColumn("KeySalt").AsCustom("text").NotNullable().WithDefaultValue(string.Empty);
  }

  public override void Down() {
    Delete.Column("KeySalt").FromTable("api_keys");
  }
}
