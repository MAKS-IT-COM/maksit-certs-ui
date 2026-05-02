using LinqToDB.Mapping;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Data;

/// <summary>
/// Builds Linq2Db mapping schema: table names from <see cref="MaksIT.CertsUI.Engine.Table"/>, columns use snake_case / lowercase identifiers aligned with Vault-style DDL.
/// </summary>
public static class CertsUILinq2DbMapping {
  static MappingSchema? _schema;
  public static MappingSchema Schema => _schema ??= Build();

  public static MappingSchema Build() {
    var schema = new MappingSchema();
    var builder = new FluentMappingBuilder(schema);

    // UserDto -> users
    builder.Entity<UserDto>()
      .HasTableName(Table.Users.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.Username).HasColumnName("username")
      .Property(x => x.Email).HasColumnName("email")
      .Property(x => x.MobileNumber).HasColumnName("mobile_number")
      .Property(x => x.IsActive).HasColumnName("is_active")
      .Property(x => x.IsGlobalAdmin).HasColumnName("is_global_admin")
      .Property(x => x.PasswordSalt).HasColumnName("password_salt")
      .Property(x => x.PasswordHash).HasColumnName("password_hash")
      .Property(x => x.TwoFactorSharedKey).HasColumnName("two_factor_shared_key")
      .Property(x => x.CreatedAt).HasColumnName("created_at")
      .Property(x => x.LastLogin).HasColumnName("last_login")
      .Property(x => x.EntityScopes).IsNotColumn()
      .Property(x => x.TwoFactorRecoveryCodes).IsNotColumn()
      .Property(x => x.JwtTokens).IsNotColumn();

    // JwtTokenDto -> jwt_tokens
    builder.Entity<JwtTokenDto>()
      .HasTableName(Table.JwtTokens.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.UserId).HasColumnName("user_id")
      .Property(x => x.Token).HasColumnName("token")
      .Property(x => x.RefreshToken).HasColumnName("refresh_token")
      .Property(x => x.IssuedAt).HasColumnName("issued_at")
      .Property(x => x.ExpiresAt).HasColumnName("expires_at")
      .Property(x => x.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at")
      .Property(x => x.IsRevoked).HasColumnName("is_revoked");

    // TwoFactorRecoveryCodeDto -> two_factor_recovery_codes
    builder.Entity<TwoFactorRecoveryCodeDto>()
      .HasTableName(Table.TwoFactorRecoveryCodes.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.UserId).HasColumnName("user_id")
      .Property(x => x.Salt).HasColumnName("salt")
      .Property(x => x.Hash).HasColumnName("hash")
      .Property(x => x.IsUsed).HasColumnName("is_used");

    // UserEntityScopeDto -> user_entity_scopes
    builder.Entity<UserEntityScopeDto>()
      .HasTableName(Table.UserEntityScopes.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.UserId).HasColumnName("user_id")
      .Property(x => x.EntityId).HasColumnName("entity_id")
      .Property(x => x.EntityType).HasColumnName("entity_type")
      .Property(x => x.Scope).HasColumnName("scope");

    // ApiKeyDto -> api_keys
    builder.Entity<ApiKeyDto>()
      .HasTableName(Table.ApiKeys.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.ApiKey).HasColumnName("api_key")
      .Property(x => x.Description).HasColumnName("description")
      .Property(x => x.IsGlobalAdmin).HasColumnName("is_global_admin")
      .Property(x => x.CreatedAt).HasColumnName("created_at")
      .Property(x => x.ExpiresAt).HasColumnName("expires_at")
      .Property(x => x.EntityScopes).IsNotColumn();

    // ApiKeyEntityScopeDto -> api_key_entity_scopes
    builder.Entity<ApiKeyEntityScopeDto>()
      .HasTableName(Table.ApiKeyEntityScopes.Name)
      .Property(x => x.Id).HasColumnName("id")
      .Property(x => x.ApiKeyId).HasColumnName("api_key_id")
      .Property(x => x.EntityId).HasColumnName("entity_id")
      .Property(x => x.EntityType).HasColumnName("entity_type")
      .Property(x => x.Scope).HasColumnName("scope");

    // RegistrationCacheDto -> registration_caches
    builder.Entity<RegistrationCacheDto>()
      .HasTableName(Table.RegistrationCaches.Name)
      .Property(x => x.Id).HasColumnName("AccountId").IsPrimaryKey()
      .Property(x => x.AccountId).IsNotColumn()
      .Property(x => x.Version).HasColumnName("Version")
      .Property(x => x.PayloadJson).HasColumnName("PayloadJson");

    builder.Entity<AcmeHttpChallengeDto>()
      .HasTableName("acme_http_challenges")
      .Property(x => x.FileName).HasColumnName("file_name").IsPrimaryKey()
      .Property(x => x.TokenValue).HasColumnName("token_value")
      .Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

    builder.Entity<TermsOfServiceCacheDto>()
      .HasTableName(Table.TermsOfServiceCache.Name)
      .Property(x => x.Url).HasColumnName("url").IsPrimaryKey()
      .Property(x => x.UrlHashHex).HasColumnName("url_hash_hex")
      .Property(x => x.ETag).HasColumnName("etag")
      .Property(x => x.LastModifiedUtc).HasColumnName("last_modified_utc")
      .Property(x => x.ContentType).HasColumnName("content_type")
      .Property(x => x.ContentBytes).HasColumnName("content_bytes")
      .Property(x => x.FetchedAtUtc).HasColumnName("fetched_at_utc")
      .Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");

    builder.Entity<AcmeSessionDto>()
      .HasTableName(Table.AcmeSessions.Name)
      .Property(x => x.SessionId).HasColumnName("session_id").IsPrimaryKey()
      .Property(x => x.AccountScopeId).HasColumnName("account_scope_id")
      .Property(x => x.PayloadJson).HasColumnName("payload_json")
      .Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc")
      .Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");

    builder.Build();
    return schema;
  }
}
