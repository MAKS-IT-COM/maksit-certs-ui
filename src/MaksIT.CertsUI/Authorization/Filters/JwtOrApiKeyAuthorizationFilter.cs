using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using MaksIT.CertsUI;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Authorization.Filters;

/// <summary>
/// Allows Bearer JWT (interactive users) or <see cref="ApiAuthConstants.ApiKeyHeaderName"/> (full API access, no scopes).
/// </summary>
public class JwtOrApiKeyAuthorizationFilter(
  ILogger<JwtOrApiKeyAuthorizationFilter> logger,
  IOptions<Configuration> appSettings,
  IIdentityDomainService identityDomainService,
  IApiKeyDomainService apiKeyDomainService
) : JwtAuthorizationFilter(logger, appSettings, identityDomainService) {

  public override async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var request = context.HttpContext.Request;

    if (request.Headers.TryGetValue(ApiAuthConstants.ApiKeyHeaderName, out var apiKeyHeader)) {
      var raw = apiKeyHeader.FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(raw)) {
        var keyResult = await apiKeyDomainService.TryValidateKeyAsync(raw, context.HttpContext.RequestAborted);
        if (!keyResult.IsSuccess) {
          context.Result = keyResult.ToActionResult();
          return;
        }
        var caller = new CallerAuth {
          IsApiKey = true,
          ApiKeyId = keyResult.Value,
          Jwt = null,
        };
        context.HttpContext.Items[HttpContextValue.CallerAuthorization] = caller;
        return;
      }
    }

    if (!request.Headers.TryGetValue(BearerTokenHeaderName, out var authorization)) {
      context.Result = Result.Forbidden("Authorization header or X-API-KEY is required.").ToActionResult();
      return;
    }

    var token = authorization.FirstOrDefault()?.Split(' ').Last();
    var validationResult = await ValidateJwtTokenAsync(token, context.HttpContext.RequestAborted);
    if (!validationResult.IsSuccess) {
      context.Result = validationResult.ToActionResult();
      return;
    }

    var tokenData = validationResult.Value!;
    context.HttpContext.Items[HttpContextValue.JwtTokenData] = tokenData;
    context.HttpContext.Items[HttpContextValue.CallerAuthorization] = new CallerAuth {
      IsApiKey = false,
      ApiKeyId = null,
      Jwt = tokenData,
    };
  }
}
