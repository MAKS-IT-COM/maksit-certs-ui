namespace MaksIT.CertsUI.Engine;

public interface IAdminUser {
  string Username { get; set; }
  string Password { get; set; }
}

public interface IJwtSettingsConfiguration {
  string JwtSecret { get; set; }
  string Issuer { get; set; }
  string Audience { get; set; }

  int ExpiresIn { get; set; }

  int RefreshTokenExpiresIn { get; set; }
  /// <summary>Pepper used for password and 2FA recovery code hashing. Not stored in DB.</summary>
  string PasswordPepper { get; set; }
}

public interface ITwoFactorSettingsConfiguration {
  string Label { get; set; }
  string Issuer { get; set; }
  string? Algorithm { get; set; }
  int? Digits { get; set; }
  int? Period { get; set; }
  int TimeTolerance { get; set; }
}

/// <summary>
/// Engine configuration (same layering as MaksIT.Vault.Engine): PostgreSQL, identity bootstrap, JWT/2FA, ACME URLs, agent reload.
/// Nested contracts <see cref="IAdminUser"/>, <see cref="IJwtSettingsConfiguration"/>, <see cref="ITwoFactorSettingsConfiguration"/> are property shapes only — resolve <see cref="ICertsEngineConfiguration"/> from DI (<c>AddCertsEngine</c>), not those facets as separate singletons.
/// </summary>
public interface ICertsEngineConfiguration {

  string ConnectionString { get; set; }

  IAdminUser Admin { get; set; }

  IJwtSettingsConfiguration JwtSettingsConfiguration { get; set; }

  ITwoFactorSettingsConfiguration TwoFactorSettingsConfiguration { get; set; }

  /// <summary>When true, add-only schema sync runs after FluentMigrator at startup.</summary>
  bool AutoSyncSchema { get; set; }

  /// <summary>Let's Encrypt production ACME directory URL (RFC 8555).</summary>
  string LetsEncryptProduction { get; set; }

  /// <summary>Let's Encrypt staging ACME directory URL.</summary>
  string LetsEncryptStaging { get; set; }

  /// <summary>Service name passed to the deployment agent after issuance (from host Agent config).</summary>
  string AgentServiceToReload { get; set; }
}
