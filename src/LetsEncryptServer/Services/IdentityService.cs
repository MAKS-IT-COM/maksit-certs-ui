using MaksIT.Core.Security.JWT;
using MaksIT.LetsEncryptServer.Domain;
using MaksIT.Results;
using Microsoft.Extensions.Options;
using Models.LetsEncryptServer.Identity.Login;
using Models.LetsEncryptServer.Identity.Logout;
using System.Security.Claims;

namespace MaksIT.LetsEncryptServer.Services;


public interface IIdentityService {
  #region Login/Refresh/Logout
  Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData);
  //Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData);
  //Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData);
  #endregion
}

public class IdentityService(
  IOptions<Configuration> appsettings,
  ISettingsService settingsService
) : IIdentityService {


  private readonly Configuration _appSettings = appsettings.Value;
  private readonly ISettingsService _settingsService = settingsService;

  #region Login/Refresh/Logout
  public async Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData) {

    var loadSettingsResult = await _settingsService.LoadAsync();
    if (!loadSettingsResult.IsSuccess || loadSettingsResult.Value == null) {
      return loadSettingsResult.ToResultOfType<LoginResponse?>(_ => null);
    }

    var settings = loadSettingsResult.Value;

    var userResult = settings.GetUserByName(requestData.Username);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<LoginResponse?>(_ => null);

    var user = userResult.Value;

    var validatePasswordResult = user.ValidatePassword(requestData.Password, _appSettings.Auth.Pepper);
    if (!validatePasswordResult.IsSuccess)
      return validatePasswordResult.ToResultOfType<LoginResponse?>(default);

    if (!JwtGenerator.TryGenerateToken(new JWTTokenGenerateRequest {
      Secret = _appSettings.Auth.Secret,
      Issuer = _appSettings.Auth.Issuer,
      Audience = _appSettings.Auth.Audience,
      Expiration = _appSettings.Auth.Expiration,
      UserId = user.Id.ToString(),
      Username = user.Name,
    }, out (string token, JWTTokenClaims claims)? tokenData, out string? errorMessage)) {
      return Result<LoginResponse?>.InternalServerError(null, errorMessage);
    }

    var (token, claims) = tokenData.Value;

    if (claims.IssuedAt == null || claims.ExpiresAt == null)
      return Result<LoginResponse?>.InternalServerError(null, "Token claims are missing required fields.");

    string refreshToken = JwtGenerator.GenerateRefreshToken();

    var response = new LoginResponse {
      TokenType = "Bearer",
      Token = token,
      ExpiresAt = claims.ExpiresAt.Value,
      RefreshToken = refreshToken,
      RefreshTokenExpiresAt = claims.IssuedAt.Value.AddDays(_appSettings.Auth.RefreshExpiration)
    };

    return Result<LoginResponse?>.Ok(response);
  }




  //public async Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData) {
  //  return await HandleTokenResponseAsync(() =>
  //    _identityDomainService.RefreshTokenAsync(requestData.RefreshToken));
  //}

  //private static async Task<Result<LoginResponse?>> HandleTokenResponseAsync(Func<Task<Result<JwtToken?>>> tokenOperation) {
  //  var jwtTokenResult = await tokenOperation();
  //  if (!jwtTokenResult.IsSuccess || jwtTokenResult.Value == null)
  //    return jwtTokenResult.ToResultOfType<LoginResponse?>(_ => null);

  //  var jwtToken = jwtTokenResult.Value;

  //  return Result<LoginResponse?>.Ok(new LoginResponse {
  //    TokenType = jwtToken.TokenType,
  //    Token = jwtToken.Token,
  //    ExpiresAt = jwtToken.ExpiresAt,
  //    RefreshToken = jwtToken.RefreshToken,
  //    RefreshTokenExpiresAt = jwtToken.RefreshTokenExpiresAt
  //  });
  //}

  //public async Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData) {
  //  var logoutResult = await _identityDomainService.LogoutAsync(jwtTokenData.Username, jwtTokenData.Token, requestData.LogoutFromAllDevices);
  //  return logoutResult;
  //}
  #endregion

}
