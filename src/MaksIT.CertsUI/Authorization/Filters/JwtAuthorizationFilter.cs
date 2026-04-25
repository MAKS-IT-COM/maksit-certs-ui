using MaksIT.CertsUI;
using MaksIT.Core.Security.JWT;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Authorization.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MaksIT.CertsUI.Authorization.Filters;

public class JwtAuthorizationFilter : BaseAsyncAuthorizationFilter {
  protected const string BearerTokenHeaderName = "Authorization";
  private readonly JwtSettingsConfiguration _jwtSettingsConfiguration;
  private readonly IIdentityDomainService _identityDomainService;

  public JwtAuthorizationFilter(
  ILogger<JwtAuthorizationFilter> logger,
  IOptions<Configuration> appSettings,
  IIdentityDomainService identityDomainService
  ) : base(logger) {
    _jwtSettingsConfiguration = appSettings.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration;
    _identityDomainService = identityDomainService;
  }

  public override async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var request = context.HttpContext.Request;
    if (!request.Headers.TryGetValue(BearerTokenHeaderName, out var authorization)) {
      context.Result = Result.Forbidden("Authorization header missing").ToActionResult();
      return;
    }

    var token = authorization.FirstOrDefault()?.Split(' ').Last();
    var validationResult = await ValidateJwtTokenAsync(token, context.HttpContext.RequestAborted);
    if (!validationResult.IsSuccess) {
      context.Result = validationResult.ToActionResult();
      return;
    }

    var tokenData = validationResult.Value;

    context.HttpContext.Items[HttpContextValue.JwtTokenData] = tokenData;
  }

  protected async Task<Result<JwtTokenData?>> ValidateJwtTokenAsync(string? token, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(token))
      return Result<JwtTokenData?>.Forbidden(null, "Token is missing");

    if (!JwtGenerator.TryValidateToken(
      _jwtSettingsConfiguration.JwtSecret,
      _jwtSettingsConfiguration.Issuer,
      _jwtSettingsConfiguration.Audience,
      token,
      out var jwtTokenClaims,
      out string? errorMessage
    )) {
      return Result<JwtTokenData?>.InternalServerError(null, errorMessage);
    }

    if (jwtTokenClaims == null ||
      jwtTokenClaims.Username == null ||
      jwtTokenClaims.Roles == null ||
      jwtTokenClaims.IssuedAt == null ||
      jwtTokenClaims.ExpiresAt == null
    ) {
      return Result<JwtTokenData?>.Forbidden(null, "Invalid JWT token or claims");
    }

    var userResult = await _identityDomainService.ReadUserByUsernameAsync(jwtTokenClaims.Username, cancellationToken);
    if (!userResult.IsSuccess || userResult.Value == null) {
      return Result<JwtTokenData?>.Forbidden(null, "User not found");
    }

    var user = userResult.Value;
    var jwtTokenData = new JwtTokenData {
      Token = token,
      Username = jwtTokenClaims.Username,
      IssuedAt = jwtTokenClaims.IssuedAt.Value,
      ExpiresAt = jwtTokenClaims.ExpiresAt.Value,
      UserId = user.Id,
    };
    return Result<JwtTokenData?>.Ok(jwtTokenData);
  }
}