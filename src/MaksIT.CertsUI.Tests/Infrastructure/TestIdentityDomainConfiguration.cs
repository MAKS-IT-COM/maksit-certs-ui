using MaksIT.CertsUI.Engine.DomainServices;

namespace MaksIT.CertsUI.Tests.Infrastructure;

public sealed class TestIdentityDomainConfiguration(
  string secret,
  string jwtIssuer,
  string audience,
  int expirationMinutes,
  int refreshExpirationDays,
  string pepper
) : IIdentityDomainConfiguration, ITwoFactorSettingsConfiguration, IDefaultAdminBootstrapConfiguration {

  public string Secret { get; } = secret;
  string IIdentityDomainConfiguration.Issuer => jwtIssuer;
  public string Audience { get; } = audience;
  public int ExpirationMinutes { get; } = expirationMinutes;
  public int RefreshExpirationDays { get; } = refreshExpirationDays;
  public string Pepper { get; } = pepper;

  public string Label { get; } = "Tests";
  string ITwoFactorSettingsConfiguration.Issuer => "TestsTotp";
  public string? Algorithm { get; } = null;
  public int? Digits { get; } = null;
  public int? Period { get; } = null;
  public int TimeTolerance { get; } = 1;

  public string Username { get; } = "admin";
  public string Password { get; } = "password";
}
