using MaksIT.Results;

namespace MaksIT.CertsUI.Authorization.Extensions;

public static class HttpContextExtension
{
    public static Result<JwtTokenData?> GetJwtTokenData(this HttpContext context)
    {
        var jwtTokenData = context.Items[HttpContextValue.JwtTokenData] as JwtTokenData;

        if (jwtTokenData == null)
            return Result<JwtTokenData?>.Forbidden(null, "JWT token data not found in the request");

        return Result<JwtTokenData?>.Ok(jwtTokenData);
    }

    public static Result<CertsUIAuthorizationData?> GetCertsUIAuthorizationData(this HttpContext context)
    {
        var certsUIAuthorizationData = context.Items[HttpContextValue.CertsUIAuthorizationData] as CertsUIAuthorizationData;

        if (certsUIAuthorizationData == null)
            return Result<CertsUIAuthorizationData?>.Forbidden(null, "CertsUI Authorization Data not found in the request");

        return Result<CertsUIAuthorizationData?>.Ok(certsUIAuthorizationData);
    }

    /// <summary>
    /// Resolves the acting principal as <see cref="JwtTokenData"/> for identity / API-key admin services.
    /// JWT sessions pass through; API keys are mapped to a synthetic principal (global admin or scoped).
    /// </summary>
    public static Result<JwtTokenData?> GetActingJwtTokenData(this HttpContext context) {
      var authResult = context.GetCertsUIAuthorizationData();
      if (!authResult.IsSuccess || authResult.Value == null)
        return authResult.ToResultOfType<JwtTokenData?>(_ => null);

      return authResult.Value.ToActingJwtTokenData();
    }
}

public static class CertsUIAuthorizationDataExtensions {
  public static Result<JwtTokenData?> ToActingJwtTokenData(this CertsUIAuthorizationData data) {
    if (data.IsJwtAuthorization)
      return Result<JwtTokenData?>.Ok(data.JwtTokenData);

    if (data.IsApiKeyAuthorization) {
      var apiKey = data.ApiKeyData!;
      var now = DateTime.UtcNow;
      return Result<JwtTokenData?>.Ok(new JwtTokenData {
        Token = string.Empty,
        Username = $"apikey:{apiKey.ApiKeyId:D}",
        ClaimRoles = [],
        IssuedAt = now,
        ExpiresAt = now.AddHours(1),
        UserId = Guid.Empty,
        IsGlobalAdmin = apiKey.IsGlobalAdmin,
        EntityScopes = apiKey.EntityScopes ?? [],
      });
    }

    return Result<JwtTokenData?>.Forbidden(null, "No valid authorization method available.");
  }
}
