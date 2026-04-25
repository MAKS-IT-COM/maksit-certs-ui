using System.Data;
using FluentMigrator;

namespace MaksIT.CertsUI.Engine.FluentMigrations;

/// <summary>
/// Normalizes JWT refresh/access tokens into <c>jwt_tokens</c> (one row per token) and drops legacy <c>users.JwtTokensJson</c>.
/// </summary>
[Migration(20260418100000)]
public class JwtTokensTableMigrateFromJson : Migration {
  public override void Up() {
    Create.Table("jwt_tokens")
      .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
      .WithColumn("UserId").AsGuid().NotNullable()
      .WithColumn("Token").AsCustom("text").NotNullable()
      .WithColumn("RefreshToken").AsCustom("text").NotNullable()
      .WithColumn("IssuedAt").AsDateTimeOffset().NotNullable()
      .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
      .WithColumn("RefreshTokenExpiresAt").AsDateTimeOffset().NotNullable()
      .WithColumn("IsRevoked").AsBoolean().NotNullable();

    Create.ForeignKey("fk_jwt_tokens_users")
      .FromTable("jwt_tokens").ForeignColumn("UserId")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.Index("IX_jwt_tokens_UserId").OnTable("jwt_tokens").OnColumn("UserId");
    Create.Index("IX_jwt_tokens_Token").OnTable("jwt_tokens").OnColumn("Token");
    Create.Index("IX_jwt_tokens_RefreshToken").OnTable("jwt_tokens").OnColumn("RefreshToken");

    Execute.Sql("""
      INSERT INTO "jwt_tokens" ("Id", "UserId", "Token", "RefreshToken", "IssuedAt", "ExpiresAt", "RefreshTokenExpiresAt", "IsRevoked")
      SELECT
        (elem->>'Id')::uuid,
        u."Id",
        COALESCE(elem->>'Token', ''),
        COALESCE(elem->>'RefreshToken', ''),
        (elem->>'IssuedAt')::timestamptz,
        (elem->>'ExpiresAt')::timestamptz,
        (elem->>'RefreshTokenExpiresAt')::timestamptz,
        COALESCE((elem->>'IsRevoked')::boolean, false)
      FROM "users" u
      CROSS JOIN LATERAL json_array_elements(
        CASE WHEN u."JwtTokensJson" IS NULL OR btrim(u."JwtTokensJson"::text) = '' THEN '[]'::json
             ELSE u."JwtTokensJson"::json END
      ) AS elem
      WHERE (elem->>'Id') IS NOT NULL
      """);

    Delete.Column("JwtTokensJson").FromTable("users");
  }

  public override void Down() =>
    throw new NotSupportedException("Restore from backup; this migration does not support rollback.");
}
