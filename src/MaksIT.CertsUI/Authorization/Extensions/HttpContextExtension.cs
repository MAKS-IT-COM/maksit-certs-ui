using MaksIT.Results;
using MaksIT.CertsUI.Authorization;

namespace MaksIT.CertsUI.Authorization.Extensions;

public static class HttpContextExtension {
  public static Result<JwtTokenData?> GetJwtTokenData(this HttpContext context) {
    var jwtTokenData = context.Items[HttpContextValue.JwtTokenData] as JwtTokenData;

    if (jwtTokenData == null)
      return Result<JwtTokenData?>.Forbidden(null, "JWT token data not found in the request");

    return Result<JwtTokenData?>.Ok(jwtTokenData);
  }

  /// <summary>JWT or validated API key (set by <see cref="Filters.JwtOrApiKeyAuthorizationFilter"/>).</summary>
  public static Result<CallerAuth?> GetCallerAuth(this HttpContext context) {
    if (context.Items[HttpContextValue.CallerAuthorization] is not CallerAuth auth)
      return Result<CallerAuth?>.Forbidden(null, "Caller authorization not found in the request.");
    if (!auth.IsApiKey && auth.Jwt == null)
      return Result<CallerAuth?>.Forbidden(null, "Caller authorization is invalid.");
    return Result<CallerAuth?>.Ok(auth);
  }
}
