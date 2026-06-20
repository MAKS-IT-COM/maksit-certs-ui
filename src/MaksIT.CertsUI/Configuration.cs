using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI;

public class Configuration {
  public required CertsEngineConfiguration CertsEngineConfiguration { get; set; }
}

public class AdminUser : IAdminUser {

  public required string Username { get; set; }

  public required string Password { get; set; }
}

public class JwtSettingsConfiguration : IJwtSettingsConfiguration {

  public required string JwtSecret { get; set; }

  public required string Issuer { get; set; }

  public required string Audience { get; set; }

  public int ExpiresIn { get; set; } = 15;

  public int RefreshTokenExpiresIn { get; set; } = 180;

  /// <summary>Pepper for password/2FA hashing. Set in appsecrets for production; empty until secrets load.</summary>
  public string PasswordPepper { get; set; } = "";
}

public class TwoFactorSettingsConfiguration : ITwoFactorSettingsConfiguration {

  public required string Label { get; set; }

  public required string Issuer { get; set; }

  public string? Algorithm { get; set; }

  public int? Digits { get; set; } = 6;

  public int? Period { get; set; } = 30;

  public int TimeTolerance { get; set; } = 1;
}

public class Agent {
  public required string AgentHostname { get; set; }

  public required int AgentPort { get; set; }

  public required string AgentKey { get; set; }

  public required string ServiceToReload { get; set; }
}

public class CertsEngineConfiguration : ICertsEngineConfiguration {

  public required string ConnectionString { get; set; }

  /// <summary>When true, add-only schema sync after FluentMigrator (ADD COLUMN only, never DROP legacy/renamed columns). Set explicitly in appsettings/Helm (deserialization defaults missing bools to false).</summary>
  public bool AutoSyncSchema { get; set; }

  public required AdminUser Admin { get; set; }

  public required JwtSettingsConfiguration JwtSettingsConfiguration { get; set; }

  public required TwoFactorSettingsConfiguration TwoFactorSettingsConfiguration { get; set; }

  public required Agent Agent { get; set; }

  /// <summary>Let's Encrypt production ACME directory URL (JSON: <c>production</c>).</summary>
  public required string Production { get; set; }

  /// <summary>Let's Encrypt staging ACME directory URL (JSON: <c>staging</c>).</summary>
  public required string Staging { get; set; }

  IAdminUser ICertsEngineConfiguration.Admin {
    get => Admin;
    set => Admin = (AdminUser)value;
  }

  IJwtSettingsConfiguration ICertsEngineConfiguration.JwtSettingsConfiguration {
    get => JwtSettingsConfiguration;
    set => JwtSettingsConfiguration = (JwtSettingsConfiguration)value;
  }

  ITwoFactorSettingsConfiguration ICertsEngineConfiguration.TwoFactorSettingsConfiguration {
    get => TwoFactorSettingsConfiguration;
    set => TwoFactorSettingsConfiguration = (TwoFactorSettingsConfiguration)value;
  }

  string ICertsEngineConfiguration.LetsEncryptProduction {
    get => Production;
    set => Production = value;
  }

  string ICertsEngineConfiguration.LetsEncryptStaging {
    get => Staging;
    set => Staging = value;
  }

  string ICertsEngineConfiguration.AgentServiceToReload {
    get => Agent.ServiceToReload;
    set => Agent.ServiceToReload = value;
  }
}
