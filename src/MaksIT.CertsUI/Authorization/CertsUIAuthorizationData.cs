using System.Diagnostics.CodeAnalysis;

namespace MaksIT.CertsUI.Authorization;

/// <summary>
/// Authorization data from the request.
/// </summary>
public class CertsUIAuthorizationData {
  public ApiKeyData? ApiKeyData { get; set; }
  public JwtTokenData? JwtTokenData { get; set; }

  [MemberNotNullWhen(true, nameof(ApiKeyData))]
  public bool IsApiKeyAuthorization => ApiKeyData != null;

  [MemberNotNullWhen(true, nameof(JwtTokenData))]
  public bool IsJwtAuthorization => JwtTokenData != null;
}
