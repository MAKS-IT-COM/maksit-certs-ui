using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

[Migration(20260427123000)]
public class TermsOfServiceCache : Migration {
  public override void Up() {
    Create.Table("terms_of_service_cache")
      .WithColumn("url").AsCustom("text").NotNullable().PrimaryKey()
      .WithColumn("url_hash_hex").AsCustom("text").NotNullable()
      .WithColumn("etag").AsCustom("text").Nullable()
      .WithColumn("last_modified_utc").AsDateTimeOffset().Nullable()
      .WithColumn("content_type").AsCustom("text").NotNullable()
      .WithColumn("content_bytes").AsCustom("bytea").NotNullable()
      .WithColumn("fetched_at_utc").AsDateTimeOffset().NotNullable()
      .WithColumn("expires_at_utc").AsDateTimeOffset().NotNullable();

    Create.Index("IX_terms_of_service_cache_url_hash_hex").OnTable("terms_of_service_cache").OnColumn("url_hash_hex");
    Create.Index("IX_terms_of_service_cache_expires_at_utc").OnTable("terms_of_service_cache").OnColumn("expires_at_utc");
  }

  public override void Down() {
    Delete.Index("IX_terms_of_service_cache_url_hash_hex").OnTable("terms_of_service_cache");
    Delete.Index("IX_terms_of_service_cache_expires_at_utc").OnTable("terms_of_service_cache");
    Delete.Table("terms_of_service_cache");
  }
}
