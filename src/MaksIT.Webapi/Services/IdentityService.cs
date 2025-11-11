
using Microsoft.Extensions.Options;
using MaksIT.Results;
using MaksIT.Webapi.Domain;
using MaksIT.Webapi.Authorization;
using MaksIT.Webapi.Abstractions.Services;
using MaksIT.Core.Security.JWT;
using MaksIT.Core.Webapi.Models;
using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;


namespace MaksIT.Webapi.Services;

public interface IIdentityService {

  #region Patch
  Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData jwtTokenData, Guid id, PatchUserRequest requestData);
  #endregion

  #region Login/Refresh/Logout
  Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData);
  Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData);
  Task<Result> Logout(LogoutRequest requestData);
  #endregion
}

public class IdentityService(
  ILogger<IdentityService> logger,
  IOptions<Configuration> appSettings,
  ISettingsService settingsService
) : ServiceBase(logger, appSettings), IIdentityService {

  #region Patch
  public async Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData jwtTokenData, Guid id, PatchUserRequest requestData) {
    var loadSettingsResult = await settingsService.LoadAsync();
    if (!loadSettingsResult.IsSuccess || loadSettingsResult.Value == null) {
      return loadSettingsResult.ToResultOfType<UserResponse?>(_ => null);
    }

    var settings = loadSettingsResult.Value;

    var userResult = settings.GetUserById(id);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<UserResponse?>(_ => null);

    var user = userResult.Value;

    #region Authentication properties
    if (requestData.TryGetOperation(nameof(requestData.Password), out var patchOperation)) {
      switch (patchOperation) {
        case PatchOperation.SetField:
          if (requestData.Password == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.Password));
          user.SetPassword(requestData.Password, _appSettings.Auth.Pepper);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }
    #endregion

    settings.UpsertUser(user);

    var saveSettingsResult = await settingsService.SaveAsync(settings);
    if (!saveSettingsResult.IsSuccess)
      return saveSettingsResult.ToResultOfType<UserResponse?>(default);

    return Result<UserResponse?>.Ok(new UserResponse {

    });

  }
  #endregion


  #region Login/Refresh/Logout
  public async Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData) {

    var loadSettingsResult = await settingsService.LoadAsync();
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

    var saveSettingsResult = await settingsService.SaveAsync(settings);
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
    var loadSettingsResult = await settingsService.LoadAsync();
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

      var saveResult = await settingsService.SaveAsync(settings);
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

    var writeResult = await settingsService.SaveAsync(settings);
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
    var loadSettingsResult = await settingsService.LoadAsync();
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
