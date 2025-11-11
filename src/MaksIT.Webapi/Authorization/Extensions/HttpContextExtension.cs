using MaksIT.Results;

namespace MaksIT.Webapi.Authorization.Extensions;

public static class HttpContextExtension {
  public static Result<JwtTokenData?> GetJwtTokenData(this HttpContext context) {
    var jwtTokenData = context.Items[HttpContextValue.JwtTokenData] as JwtTokenData;

    if (jwtTokenData == null)
      return Result<JwtTokenData?>.Forbidden(null, "JWT token data not found in the request");

    return Result<JwtTokenData?>.Ok(jwtTokenData);
  }
}
