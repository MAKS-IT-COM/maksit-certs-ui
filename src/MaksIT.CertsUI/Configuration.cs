using MaksIT.CertsUI.Engine.DomainServices;

namespace MaksIT.CertsUI;

public class Configuration {
  public required CertsUIEngineConfiguration CertsUIEngineConfiguration { get; set; }
}

public class AdminUser : IDefaultAdminBootstrapConfiguration {

  public required string Username { get; set; }

  public required string Password { get; set; }
}

public class JwtSettingsConfiguration : IIdentityDomainConfiguration {

  public required string JwtSecret { get; set; }

  public required string Issuer { get; set; }

  public required string Audience { get; set; }

  public int ExpiresIn { get; set; }

  public int RefreshTokenExpiresIn { get; set; }

  /// <summary>Pepper for password/2FA hashing. If not set, defaults to empty (set in appsecrets for production).</summary>
  public string PasswordPepper { get; set; } = "";

  string IIdentityDomainConfiguration.Secret => JwtSecret;

  string IIdentityDomainConfiguration.Issuer => Issuer;

  string IIdentityDomainConfiguration.Audience => Audience;

  int IIdentityDomainConfiguration.ExpirationMinutes => ExpiresIn;

  int IIdentityDomainConfiguration.RefreshExpirationDays => RefreshTokenExpiresIn;

  string IIdentityDomainConfiguration.Pepper => PasswordPepper;
}

public class TwoFactorSettingsConfiguration : ITwoFactorSettingsConfiguration {

  public required string Label { get; set; }

  public required string Issuer { get; set; }

  public string? Algorithm { get; set; }

  public int? Digits { get; set; }

  public int? Period { get; set; }

  public int TimeTolerance { get; set; }
}

public class Agent {
  public required string AgentHostname { get; set; }

  public required int AgentPort { get; set; }

  public required string AgentKey { get; set; }

  public required string ServiceToReload { get; set; }
}

public class CertsUIEngineConfiguration : ICertsFlowEngineConfiguration {

  /// <summary>Npgsql connection string; optional when using legacy <c>ConnectionStrings:Certs</c>.</summary>
  public string? ConnectionString { get; set; }

  /// <summary>When true, add-only schema sync after FluentMigrator (ADD COLUMN only, never DROP legacy/renamed columns). Set explicitly in appsettings/Helm (deserialization defaults missing bools to false).</summary>
  public bool AutoSyncSchema { get; set; }

  public required AdminUser Admin { get; set; }

  public required JwtSettingsConfiguration JwtSettingsConfiguration { get; set; }

  public required TwoFactorSettingsConfiguration TwoFactorSettingsConfiguration { get; set; }

  public required Agent Agent { get; set; }

  public required string Production { get; set; }

  public required string Staging { get; set; }

  string ICertsFlowEngineConfiguration.AgentServiceToReload => Agent.ServiceToReload;
}
