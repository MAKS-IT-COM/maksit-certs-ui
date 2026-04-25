using Microsoft.Extensions.Options;
using MaksIT.Core.Security;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;
using MaksIT.Models.LetsEncryptServer.Identity.User.Search;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;
using DomainUser = MaksIT.CertsUI.Engine.Domain.Identity.User;

namespace MaksIT.CertsUI.Services;

public interface IIdentityService {
  Task<Result<MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse>>> SearchUsersAsync(JwtTokenData jwtTokenData, SearchUserRequest requestData);
  Task<Result<UserResponse?>> ReadUserAsync(JwtTokenData jwtTokenData, Guid id);
  Task<Result<UserResponse?>> PostUserAsync(JwtTokenData jwtTokenData, CreateUserRequest requestData);
  Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData jwtTokenData, Guid id, PatchUserRequest requestData);
  Task<Result> DeleteUserAsync(JwtTokenData jwtTokenData, Guid id);
  Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData);
  Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData);
  Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData);
}

public sealed class IdentityService(
  ILogger<IdentityService> logger,
  IOptions<Configuration> appSettings,
  IIdentityDomainService identityDomainService,
  IUserQueryService userQueryService,
  UserToResponseMapper userToResponseMapper,
  ITwoFactorSettingsConfiguration twoFactorSettings
) : ServiceBase(logger, appSettings), IIdentityService {

  private readonly ITwoFactorSettingsConfiguration _twoFactorSettings = twoFactorSettings;

  public async Task<Result<MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse>>> SearchUsersAsync(JwtTokenData _jwtTokenData, SearchUserRequest requestData) {
    _ = _jwtTokenData;
    var page = Math.Max(1, requestData.PageNumber);
    var size = Math.Clamp(requestData.PageSize, 1, 500);

    var query = await userQueryService.SearchUsersAsync(requestData.UsernameFilter?.Trim(), page, size);
    if (!query.IsSuccess || query.Value == null)
      return query.ToResultOfType<MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse>>(_ => new MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse> {
        Data = [],
        TotalRecords = 0,
        PageNumber = page,
        PageSize = size,
      })!;

    var paged = query.Value;
    return Result<MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse>>.Ok(new MaksIT.Models.LetsEncryptServer.Common.PagedResponse<SearchUserResponse> {
      Data = [.. paged.Data.Select(userToResponseMapper.MapToSearchResponse)],
      TotalRecords = paged.TotalRecords,
      PageNumber = paged.PageNumber,
      PageSize = paged.PageSize,
    });
  }

  public async Task<Result<UserResponse?>> ReadUserAsync(JwtTokenData _jwtTokenData, Guid id) {
    _ = _jwtTokenData;
    var userResult = await identityDomainService.ReadUserByIdAsync(id);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<UserResponse?>(_ => null);

    return Result<UserResponse?>.Ok(userToResponseMapper.MapToResponse(userResult.Value));
  }

  public async Task<Result<UserResponse?>> PostUserAsync(JwtTokenData _jwtTokenData, CreateUserRequest requestData) {
    _ = _jwtTokenData;
    var created = await identityDomainService.CreateUserAsync(requestData.Username, requestData.Password);
    if (!created.IsSuccess || created.Value == null)
      return created.ToResultOfType<UserResponse?>(_ => null);

    return Result<UserResponse?>.Ok(userToResponseMapper.MapToResponse(created.Value));
  }

  #region Patch

  public async Task<Result<UserResponse?>> PatchUserAsync(JwtTokenData _jwtTokenData, Guid id, PatchUserRequest requestData) {
    _ = _jwtTokenData;

    var userResult = await identityDomainService.ReadUserByIdAsync(id);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult.ToResultOfType<UserResponse?>(_ => null);

    var user = userResult.Value;

    var hasTwoFactorToggle = requestData.TwoFactorEnabled == true || requestData.TwoFactorEnabled == false;
    var hasOperations = requestData.Operations != null && requestData.Operations.Count > 0;
    if (!hasTwoFactorToggle && !hasOperations)
      return Result<UserResponse?>.Ok(userToResponseMapper.MapToResponse(user));

    var masterDataResult = PatchUserMasterData(user, requestData);
    if (!masterDataResult.IsSuccess)
      return masterDataResult;

    var authPropsResult = PatchUserAuthentication(user, requestData);
    if (!authPropsResult.IsSuccess)
      return authPropsResult;

    var twoFactorResult = PatchUserTwoFactor(user, requestData, out var twoFactorRecoveryCodes);
    if (!twoFactorResult.IsSuccess)
      return twoFactorResult;

    var saveResult = await identityDomainService.WriteUserAsync(user);
    if (!saveResult.IsSuccess || saveResult.Value == null)
      return saveResult.ToResultOfType<UserResponse?>(_ => null);

    user = saveResult.Value;

    if (twoFactorRecoveryCodes != null) {
      var userResponse = userToResponseMapper.MapToResponse(user);
      if (!TotpGenerator.TryGenerateTotpAuthLink(
          _twoFactorSettings.Label,
          user.Username,
          user.TwoFactorSharedKey ?? "",
          _twoFactorSettings.Issuer,
          _twoFactorSettings.Algorithm,
          _twoFactorSettings.Digits,
          _twoFactorSettings.Period,
          out var authLink,
          out var errorMessage)) {
        _logger.LogError("{Message}", errorMessage);
        return Result<UserResponse?>.InternalServerError(null, errorMessage);
      }
      userResponse.QrCodeUrl = authLink;
      userResponse.TwoFactorRecoveryCodes = twoFactorRecoveryCodes;
      userResponse.RecoveryCodesLeft = user.TwoFactorRecoveryCodes.Count(x => !x.IsUsed);
      return Result<UserResponse?>.Ok(userResponse);
    }

    return Result<UserResponse?>.Ok(userToResponseMapper.MapToResponse(user));
  }

  /// <summary>Applies <see cref="PatchUserRequest.IsActive"/> when present in patch operations.</summary>
  private Result<UserResponse?> PatchUserMasterData(DomainUser user, PatchUserRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.IsActive), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.IsActive == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.IsActive));
          user.SetIsActive(requestData.IsActive.Value);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    return Result<UserResponse?>.Ok(null);
  }

  /// <summary>Applies password change when present in patch operations.</summary>
  private Result<UserResponse?> PatchUserAuthentication(DomainUser user, PatchUserRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.Password), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.Password == null)
            return PatchFieldIsNotDefined<UserResponse?>(nameof(requestData.Password));
          var setPasswordResult = user.SetPassword(requestData.Password, _appSettings.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper);
          if (!setPasswordResult.IsSuccess || setPasswordResult.Value == null)
            return setPasswordResult.ToResultOfType<UserResponse?>(_ => null);
          break;
        default:
          return UnsupportedPatchOperationResponse<UserResponse?>();
      }
    }

    return Result<UserResponse?>.Ok(null);
  }

  /// <summary>Enables or disables 2FA from <see cref="PatchUserRequest.TwoFactorEnabled"/> (Vault parity: not gated on <c>Operations</c>).</summary>
  private Result<UserResponse?> PatchUserTwoFactor(DomainUser user, PatchUserRequest requestData, out List<string>? twoFactorRecoveryCodes) {
    twoFactorRecoveryCodes = null;

    if (requestData.TwoFactorEnabled == true) {
      var enableTwoFactorAuthResult = identityDomainService.EnableTwoFactorAuthForUser(user);
      if (!enableTwoFactorAuthResult.IsSuccess)
        return enableTwoFactorAuthResult.ToResultOfType<UserResponse?>(_ => null);

      twoFactorRecoveryCodes = enableTwoFactorAuthResult.Value;
    }
    else if (requestData.TwoFactorEnabled == false) {
      var disableResult = user.DisableTwoFactorAuth();
      if (!disableResult.IsSuccess)
        return disableResult.ToResultOfType<UserResponse?>(_ => null);
    }

    return Result<UserResponse?>.Ok(null);
  }

  #endregion

  public async Task<Result> DeleteUserAsync(JwtTokenData jwtTokenData, Guid id) {
    if (id == jwtTokenData.UserId)
      return Result.BadRequest("Cannot delete your own user account.");

    return await identityDomainService.DeleteUserAsync(id);
  }

  public async Task<Result<LoginResponse?>> LoginAsync(LoginRequest requestData) {
    var login = await identityDomainService.LoginAsync(
      requestData.Username,
      requestData.Password,
      requestData.TwoFactorCode,
      requestData.TwoFactorRecoveryCode);
    if (!login.IsSuccess || login.Value == null)
      return login.ToResultOfType<LoginResponse?>(_ => null);

    return Result<LoginResponse?>.Ok(MapToLoginResponse(login.Value));
  }

  public async Task<Result<LoginResponse?>> RefreshTokenAsync(RefreshTokenRequest requestData) {
    var refresh = await identityDomainService.RefreshTokenAsync(requestData.RefreshToken);
    if (!refresh.IsSuccess || refresh.Value == null)
      return refresh.ToResultOfType<LoginResponse?>(_ => null);

    return Result<LoginResponse?>.Ok(MapToLoginResponse(refresh.Value));
  }

  public Task<Result> Logout(JwtTokenData jwtTokenData, LogoutRequest requestData) =>
    identityDomainService.LogoutAsync(jwtTokenData.Token, requestData.LogoutFromAllDevices);

  private static LoginResponse MapToLoginResponse(LoginDomainResult value) => new() {
    TokenType = value.TokenType,
    Token = value.Token,
    ExpiresAt = value.ExpiresAt,
    RefreshToken = value.RefreshToken,
    RefreshTokenExpiresAt = value.RefreshTokenExpiresAt,
    Username = value.Username,
  };
}
