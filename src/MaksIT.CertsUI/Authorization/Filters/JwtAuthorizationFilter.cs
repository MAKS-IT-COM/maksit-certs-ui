using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Core.Security.JWT;
using MaksIT.CertsUI.Abstractions.Authorization.Filters;


namespace MaksIT.CertsUI.Authorization.Filters;

public class JwtAuthorizationFilter : BaseAsyncAuthorizationFilter {
  private const string BearerTokenHeaderName = "Authorization"; // JWT header

  private readonly JwtSettingsConfiguration _jwtSettingsConfiguration;
  private readonly IIdentityDomainService _identityDomainService;

  public JwtAuthorizationFilter(
      ILogger<JwtAuthorizationFilter> logger,
      IOptions<Configuration> configuration,
      IIdentityDomainService identityDomainService
  ) : base(logger) {
    _jwtSettingsConfiguration = configuration.Value.CertsEngineConfiguration.JwtSettingsConfiguration;
    _identityDomainService = identityDomainService;
  }

  public override async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var request = context.HttpContext.Request;
    if (!request.Headers.TryGetValue(BearerTokenHeaderName, out var authorization)) {
      context.Result = Result.Forbidden("Authorization header missing").ToActionResult();
      return;
    }

    var token = authorization.FirstOrDefault()?.Split(' ').Last();
    var validationResult = await ValidateJwtTokenAsync(token);
    if (!validationResult.IsSuccess) {
      context.Result = validationResult.ToActionResult();
      return;
    }

    var jwtTokenData = validationResult.Value;
    context.HttpContext.Items[HttpContextValue.JwtTokenData] = jwtTokenData;
  }

  protected Task<Result<JwtTokenData?>> ValidateJwtTokenAsync(string? token) {
    if (string.IsNullOrWhiteSpace(token))
      return Task.FromResult(Result<JwtTokenData?>.Forbidden(null, "Token is missing"));

    if (!JwtGenerator.TryValidateToken(
            _jwtSettingsConfiguration.JwtSecret,
            _jwtSettingsConfiguration.Issuer,
            _jwtSettingsConfiguration.Audience,
            token,
            out var jwtTokenClaims,
            out string? errorMessage)) {
      return Task.FromResult(Result<JwtTokenData?>.InternalServerError(null, errorMessage));
    }

    if (jwtTokenClaims == null ||
        jwtTokenClaims.Username == null ||
        jwtTokenClaims.Roles == null ||
        jwtTokenClaims.IssuedAt == null ||
        jwtTokenClaims.ExpiresAt == null) {
      return Task.FromResult(Result<JwtTokenData?>.Forbidden(null, "Invalid JWT token or claims"));
    }

    var userResult = _identityDomainService.ReadUserByUsername(jwtTokenClaims.Username);
    if (!userResult.IsSuccess || userResult.Value == null) {
      return Task.FromResult(Result<JwtTokenData?>.Forbidden(null, "User not found"));
    }

    var user = userResult.Value;
    var authResult = _identityDomainService.ReadUserAuthorization(user.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;

    var jwtTokenData = new JwtTokenData {
      Token = token!,
      Username = jwtTokenClaims.Username,
      ClaimRoles = jwtTokenClaims.Roles,
      IssuedAt = jwtTokenClaims.IssuedAt.Value,
      ExpiresAt = jwtTokenClaims.ExpiresAt.Value,

      UserId = user.Id,
      IsGlobalAdmin = authorization?.IsGlobalAdmin ?? false,
      EntityScopes = authorization?.EntityScopes == null
        ? []
        : [.. authorization.EntityScopes.Select(s => new IdentityScopeData {
          EntityId = s.EntityId,
          EntityType = s.EntityType,
          Scope = s.Scope
        })],
    };
    return Task.FromResult(Result<JwtTokenData?>.Ok(jwtTokenData));
  }
}
