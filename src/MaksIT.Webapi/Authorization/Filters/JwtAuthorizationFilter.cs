using MaksIT.Core.Security.JWT;
using MaksIT.Results;
using MaksIT.Webapi.Abstractions.Authorization.Filters;
using MaksIT.Webapi.Dto;
using MaksIT.Webapi.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MaksIT.Webapi.Authorization.Filters;

public class JwtAuthorizationFilter : BaseAsyncAuthorizationFilter {
  private const string BearerTokenHeaderName = "Authorization";
  private readonly Auth _jwtSettingsConfiguration;
  private readonly ISettingsService _settingsService;

  public JwtAuthorizationFilter(
  ILogger<JwtAuthorizationFilter> logger,
  IOptions<Configuration> appSettings,
  ISettingsService settingsService
  ) : base(logger) {
    _jwtSettingsConfiguration = appSettings.Value.Auth;
    _settingsService = settingsService;
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

    var tokenData = validationResult.Value;

    context.HttpContext.Items[HttpContextValue.JwtTokenData] = tokenData;
  }

  private async Task<Result<JwtTokenData?>> ValidateJwtTokenAsync(string? token) {
    if (string.IsNullOrWhiteSpace(token))
      return Result<JwtTokenData?>.Forbidden(null, "Token is missing");

    if (!JwtGenerator.TryValidateToken(
      _jwtSettingsConfiguration.Secret,
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

    var settingsResult = await _settingsService.LoadAsync();
    if (!settingsResult.IsSuccess || settingsResult.Value == null) {
      return Result<JwtTokenData?>.InternalServerError(null, "Failed to load settings");
    }

    var settings = settingsResult.Value;
    var userResult = settings.GetUserByName(jwtTokenClaims.Username);
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