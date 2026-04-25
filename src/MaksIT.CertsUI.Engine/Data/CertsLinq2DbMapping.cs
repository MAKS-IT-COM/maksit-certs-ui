using LinqToDB.Mapping;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Data;

/// <summary>
/// Builds Linq2Db mapping schema: table names from <see cref="MaksIT.CertsUI.Engine.Table"/>, columns aligned with the FluentMigrator baseline.
/// </summary>
public static class CertsLinq2DbMapping {
  static MappingSchema? _schema;
  public static MappingSchema Schema => _schema ??= Build();

  public static MappingSchema Build() {
    var schema = new MappingSchema();
    var builder = new FluentMappingBuilder(schema);

    // UserDto -> users
    builder.Entity<UserDto>()
      .HasTableName(Table.Users.Name)
      .Property(x => x.Id).HasColumnName("Id").IsPrimaryKey()
      .Property(x => x.Name).HasColumnName("Name")
      .Property(x => x.Salt).HasColumnName("Salt")
      .Property(x => x.Hash).HasColumnName("Hash")
      .Property(x => x.LastLoginUtc).HasColumnName("LastLoginUtc")
      .Property(x => x.IsActive).HasColumnName("IsActive")
      .Property(x => x.TwoFactorSharedKey).HasColumnName("TwoFactorSharedKey")
      .Property(x => x.JwtTokens).IsNotColumn()
      .Property(x => x.TwoFactorRecoveryCodes).IsNotColumn();

    builder.Entity<TwoFactorRecoveryCodeDto>()
      .HasTableName(Table.TwoFactorRecoveryCodes.Name)
      .Property(x => x.Id).HasColumnName("Id").IsPrimaryKey()
      .Property(x => x.UserId).HasColumnName("UserId")
      .Property(x => x.Salt).HasColumnName("Salt")
      .Property(x => x.Hash).HasColumnName("Hash")
      .Property(x => x.IsUsed).HasColumnName("IsUsed");

    // JwtTokenDto -> jwt_tokens
    builder.Entity<JwtTokenDto>()
      .HasTableName(Table.JwtTokens.Name)
      .Property(x => x.Id).HasColumnName("Id").IsPrimaryKey()
      .Property(x => x.UserId).HasColumnName("UserId")
      .Property(x => x.Token).HasColumnName("Token")
      .Property(x => x.RefreshToken).HasColumnName("RefreshToken")
      .Property(x => x.IssuedAt).HasColumnName("IssuedAt")
      .Property(x => x.ExpiresAt).HasColumnName("ExpiresAt")
      .Property(x => x.RefreshTokenExpiresAt).HasColumnName("RefreshTokenExpiresAt")
      .Property(x => x.IsRevoked).HasColumnName("IsRevoked");

    // ApiKeyDto -> api_keys
    builder.Entity<ApiKeyDto>()
      .HasTableName(Table.ApiKeys.Name)
      .Property(x => x.Id).HasColumnName("Id").IsPrimaryKey()
      .Property(x => x.Description).HasColumnName("Description")
      .Property(x => x.ExpiresAtUtc).HasColumnName("ExpiresAtUtc")
      .Property(x => x.KeySalt).HasColumnName("KeySalt")
      .Property(x => x.KeyHashHex).HasColumnName("KeyHashHex")
      .Property(x => x.CreatedAtUtc).HasColumnName("CreatedAtUtc")
      .Property(x => x.RevokedAtUtc).HasColumnName("RevokedAtUtc");

    // RegistrationCacheDto -> registration_caches
    builder.Entity<RegistrationCacheDto>()
      .HasTableName(Table.RegistrationCaches.Name)
      .Property(x => x.AccountId).HasColumnName("AccountId").IsPrimaryKey()
      .Property(x => x.Version).HasColumnName("Version")
      .Property(x => x.PayloadJson).HasColumnName("PayloadJson");

    builder.Entity<AcmeHttpChallengeDto>()
      .HasTableName("acme_http_challenges")
      .Property(x => x.FileName).HasColumnName("file_name").IsPrimaryKey()
      .Property(x => x.TokenValue).HasColumnName("token_value")
      .Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

    builder.Build();
    return schema;
  }
}
