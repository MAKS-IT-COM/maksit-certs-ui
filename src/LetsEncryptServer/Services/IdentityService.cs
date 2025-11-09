using MaksIT.Core.Security.JWT;
using MaksIT.LetsEncryptServer.Domain;
using MaksIT.Results;
using Microsoft.Extensions.Options;
using Models.LetsEncryptServer.Identity.Login;
using Models.LetsEncryptServer.Identity.Logout;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Security.Claims;

namespace MaksIT.LetsEncryptServer.Services;


public interface IIdentityService {
  #region Login/Refresh/Logout
  Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData);
  Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData);
  Task<Result> Logout(LogoutRequest requestData);
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

    var tokenDomain = new JwtToken()
      .SetAccessTokenData(token, claims.IssuedAt.Value, claims.ExpiresAt.Value)
      .SetRefreshTokenData(refreshToken, claims.IssuedAt.Value.AddDays(_appSettings.Auth.RefreshExpiration));

    user.UpsertJwtToken(tokenDomain);
    user.SetLastLogin();
    settings.UpsertUser(user);

    var saveSettingsResult = await _settingsService.SaveAsync(settings);
    if (!saveSettingsResult.IsSuccess)
      return saveSettingsResult.ToResultOfType<LoginResponse?>(default);

    var response = new LoginResponse {
      TokenType = tokenDomain.TokenType,
      Token = tokenDomain.Token,
      ExpiresAt = claims.ExpiresAt.Value,
      RefreshToken = tokenDomain.RefreshToken,
      RefreshTokenExpiresAt = tokenDomain.RefreshTokenExpiresAt
    };

    return Result<LoginResponse?>.Ok(response);
  }

  public async Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData) {
    var loadSettingsResult = await _settingsService.LoadAsync();
    if (!loadSettingsResult.IsSuccess || loadSettingsResult.Value == null)
        return loadSettingsResult.ToResultOfType<LoginResponse?>(_ => null);

    var settings = loadSettingsResult.Value;
    var userResult = settings.GetByRefreshToken(requestData.RefreshToken);
    if (!userResult.IsSuccess || userResult.Value == null)
        return Result<LoginResponse?>.Unauthorized(null, "Invalid refresh token.");

    var user = userResult.Value.RemoveRevokedJwtTokens();
    var tokenDomain = user.JwtTokens.SingleOrDefault(t => t.RefreshToken == requestData.RefreshToken);

    if (tokenDomain == null)
        return Result<LoginResponse?>.Unauthorized(null, "Invalid refresh token.");

    // Token is still valid
    if (DateTime.UtcNow <= tokenDomain.ExpiresAt) {
        user.SetLastLogin();
        settings.UpsertUser(user);

        var saveResult = await _settingsService.SaveAsync(settings);
        if (!saveResult.IsSuccess)
            return saveResult.ToResultOfType<LoginResponse?>(default);

        return Result<LoginResponse?>.Ok(new LoginResponse {
            TokenType = tokenDomain.TokenType,
            Token = tokenDomain.Token,
            ExpiresAt = tokenDomain.ExpiresAt,
            RefreshToken = tokenDomain.RefreshToken,
            RefreshTokenExpiresAt = tokenDomain.RefreshTokenExpiresAt
        });
    }

    // Refresh token expired
    if (DateTime.UtcNow > tokenDomain.RefreshTokenExpiresAt) {
        user.RemoveJwtToken(tokenDomain.Id);
        return Result<LoginResponse?>.Unauthorized(null, "Refresh token has expired.");
    }

    // Refresh token is valid - generate new tokens

    if (!JwtGenerator.TryGenerateToken(new JWTTokenGenerateRequest {
        Secret = _appSettings.Auth.Secret,
        Issuer = _appSettings.Auth.Issuer,
        Audience = _appSettings.Auth.Audience,
        Expiration = _appSettings.Auth.Expiration,
        UserId = user.Id.ToString(),
        Username = user.Name,
    }, out (string token, JWTTokenClaims claims)? tokenData, out string? errorMessage))
        return Result<LoginResponse?>.InternalServerError(null, errorMessage);

    var (token, claims) = tokenData.Value;

    if (claims.IssuedAt == null || claims.ExpiresAt == null)
        return Result<LoginResponse?>.InternalServerError(null, "Token claims are missing required fields.");

    string refreshToken = JwtGenerator.GenerateRefreshToken();

    tokenDomain = new JwtToken()
        .SetAccessTokenData(token, claims.IssuedAt.Value, claims.ExpiresAt.Value)
        .SetRefreshTokenData(refreshToken, claims.IssuedAt.Value.AddDays(_appSettings.Auth.RefreshExpiration));

    user.UpsertJwtToken(tokenDomain);
    user.SetLastLogin();
    settings.UpsertUser(user);

    var writeResult = await _settingsService.SaveAsync(settings);
    if (!writeResult.IsSuccess)
        return writeResult.ToResultOfType<LoginResponse?>(default);

    return Result<LoginResponse?>.Ok(new LoginResponse {
        TokenType = tokenDomain.TokenType,
        Token = tokenDomain.Token,
        ExpiresAt = claims.ExpiresAt.Value,
        RefreshToken = tokenDomain.RefreshToken,
        RefreshTokenExpiresAt = tokenDomain.RefreshTokenExpiresAt
    });
  }

  public async Task<Result> Logout(LogoutRequest requestData) {
    var loadSettingsResult = await _settingsService.LoadAsync();
    if (!loadSettingsResult.IsSuccess || loadSettingsResult.Value == null)
      return loadSettingsResult.ToResultOfType<LoginResponse?>(_ => null);

    var settings = loadSettingsResult.Value;

    var userResult = settings.GetByJwtToken(requestData.Token);
    if (userResult.IsSuccess && userResult.Value != null) {
      var user = userResult.Value;

      if (requestData.LogoutFromAllDevices)
        user.RevokeAllJwtTokens();
      else
        user.RemoveJwtToken(requestData.Token);

      settings.UpsertUser(user);

      var writeUserResult = await settingsService.SaveAsync(settings);
    }

    return Result.Ok();
  }
  #endregion

}
