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
}
