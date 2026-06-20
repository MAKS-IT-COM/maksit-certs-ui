using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.DomainServices;


namespace MaksIT.CertsUI.Authorization.Filters {
  public class CertsUIAuthorizationFilter(
    IOptions<Configuration> configuration,
    IApiKeyDomainService apiKeyDomainService,
    IIdentityDomainService identityDomainService
  ) : JwtAuthorizationFilter(configuration, identityDomainService) {
    private const string BearerTokenHeaderName = "Authorization"; // JWT header
    private const string ApiKeyHeaderName = "X-API-KEY";            // API Key header

    public override async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
      var request = context.HttpContext.Request;

      // Attempt API Key authentication first.
      if (request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue)) {
        var apiKeyResult = await ValidateApiKeyAsync(apiKeyValue);
        if (apiKeyResult.IsSuccess && apiKeyResult.Value != null) {
          context.HttpContext.Items[HttpContextValue.CertsUIAuthorizationData] = new CertsUIAuthorizationData {
            ApiKeyData = apiKeyResult.Value
          };
          return;
        }
        context.Result = apiKeyResult.ToActionResult();
        return;
      }

      // Fallback to JWT authentication.
      if (request.Headers.TryGetValue(BearerTokenHeaderName, out var authorization)) {
        var token = authorization.FirstOrDefault()?.Split(' ').Last();
        var jwtResult = await ValidateJwtTokenAsync(token);
        if (jwtResult.IsSuccess && jwtResult.Value != null) {
          context.HttpContext.Items[HttpContextValue.CertsUIAuthorizationData] = new CertsUIAuthorizationData {
            JwtTokenData = jwtResult.Value,
          };
          return;
        }
        context.Result = jwtResult.ToActionResult();
        return;
      }

      // Neither API Key nor valid JWT provided.
      context.Result = Result.Forbidden("Authorization required").ToActionResult();
    }

    private Task<Result<ApiKeyData?>> ValidateApiKeyAsync(string? apiKeyValue) {
      if (string.IsNullOrWhiteSpace(apiKeyValue)) {
        return Task.FromResult(Result<ApiKeyData?>.Forbidden(null, "Invalid API Key"));
      }

      var apiKeyResult = apiKeyDomainService.ReadAPIKey(apiKeyValue!);
      if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null) {
        return Task.FromResult(Result<ApiKeyData?>.Forbidden(null, "Invalid API Key"));
      }

      var apiKey = apiKeyResult.Value;
      if (apiKey.ExpiresAt <= DateTime.UtcNow) {
        return Task.FromResult(Result<ApiKeyData?>.Forbidden(null, "API Key expired"));
      }

      var authResult = apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
      if (!authResult.IsSuccess || authResult.Value == null) {
        return Task.FromResult(authResult.ToResultOfType<ApiKeyData?>(_ => null));
      }

      var authorization = authResult.Value;
      var apiKeyData = new ApiKeyData {
        ApiKeyId = apiKey.Id,
        IsGlobalAdmin = authorization.IsGlobalAdmin,
        EntityScopes = authorization.EntityScopes == null
          ? []
          : [.. authorization.EntityScopes.Select(scope => new IdentityScopeData {
            EntityId = scope.EntityId,
            EntityType = scope.EntityType,
            Scope = scope.Scope
          })]
      };
      return Task.FromResult(Result<ApiKeyData?>.Ok(apiKeyData));
    }
  }
}
