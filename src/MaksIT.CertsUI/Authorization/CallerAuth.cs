namespace MaksIT.CertsUI.Authorization;

/// <summary>
/// Request authentication: interactive JWT or service <c>X-API-KEY</c> (no scopes).
/// </summary>
public sealed class CallerAuth {
  public required bool IsApiKey { get; init; }
  public Guid? ApiKeyId { get; init; }
  public JwtTokenData? Jwt { get; init; }
}
