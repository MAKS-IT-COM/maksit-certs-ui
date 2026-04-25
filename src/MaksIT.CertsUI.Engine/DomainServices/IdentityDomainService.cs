using MaksIT.Core.Security;
using MaksIT.Core.Security.JWT;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.DomainServices;

public interface IIdentityDomainConfiguration {
  string Secret { get; }
  string Issuer { get; }
  string Audience { get; }
  int ExpirationMinutes { get; }
  int RefreshExpirationDays { get; }
  string Pepper { get; }
}

/// <summary>TOTP / otpauth label settings (Vault: <c>ITwoFactorSettingsConfiguration</c>).</summary>
public interface ITwoFactorSettingsConfiguration {
  string Label { get; }
  string Issuer { get; }
  string? Algorithm { get; }
  int? Digits { get; }
  int? Period { get; }
  int TimeTolerance { get; }
}

public class LoginDomainResult {
  public required string TokenType { get; set; }
  public required string Token { get; set; }
  public required DateTime ExpiresAt { get; set; }
  public required string RefreshToken { get; set; }
  public required DateTime RefreshTokenExpiresAt { get; set; }
  public required string Username { get; set; }
}

public interface IIdentityDomainService {

  #region Read
  Task<Result<int>> CountUsersAsync(CancellationToken cancellationToken = default);
  Task<Result<User?>> ReadUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
  Task<Result<User?>> ReadUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
  Task<Result<User?>> ReadUserByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
  #endregion

  #region Write
  /// <summary>Persists the user aggregate (tokens and credentials) as provided.</summary>
  Task<Result<User?>> WriteUserAsync(User user, CancellationToken cancellationToken = default);
  Task<Result<User?>> CreateUserAsync(string username, string password, CancellationToken cancellationToken = default);
  Task<Result<User?>> ChangePasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
  #endregion

  #region Delete
  Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
  #endregion

  #region First boot Initialize
  Task<Result> EnsureDefaultAdminAsync(CancellationToken cancellationToken = default);
  #endregion

  #region Login/Refresh/Logout
  Task<Result<LoginDomainResult?>> LoginAsync(string username, string password, string? twoFactorCode = null, string? twoFactorRecoveryCode = null, CancellationToken cancellationToken = default);
  Task<Result<LoginDomainResult?>> RefreshTokenAsync(string refreshToken, bool? force = false, CancellationToken cancellationToken = default);
  Task<Result> LogoutAsync(string accessToken, bool logoutFromAllDevices, CancellationToken cancellationToken = default);
  #endregion

  #region Two-factor authentication
  /// <summary>Generates TOTP secret and recovery codes; mutates <paramref name="user"/>.</summary>
  Result<List<string>?> EnableTwoFactorAuthForUser(User user);
  #endregion
}

public class IdentityDomainService(
  ILogger<IdentityDomainService> logger,
  IIdentityPersistanceService userPersistence,
  IIdentityDomainConfiguration config,
  ITwoFactorSettingsConfiguration twoFactorSettings,
  IDefaultAdminBootstrapConfiguration defaultAdminBootstrap
) : IIdentityDomainService {

  private readonly ILogger<IdentityDomainService> _logger = logger;
  private readonly IIdentityPersistanceService _userPersistence = userPersistence;
  private readonly IIdentityDomainConfiguration _config = config;
  private readonly ITwoFactorSettingsConfiguration _twoFactorSettings = twoFactorSettings;
  private readonly IDefaultAdminBootstrapConfiguration _defaultAdminBootstrap = defaultAdminBootstrap;

  private const int TwoFactorRecoveryCodeCount = 5;

  #region Read
  public async Task<Result<int>> CountUsersAsync(CancellationToken cancellationToken = default) =>
    Result<int>.Ok(await _userPersistence.CountAsync(cancellationToken));

  public Task<Result<User?>> ReadUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
    _userPersistence.GetByIdAsync(userId, cancellationToken);

  public Task<Result<User?>> ReadUserByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
    _userPersistence.GetByNameAsync(username, cancellationToken);

  public Task<Result<User?>> ReadUserByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
    _userPersistence.GetByAccessTokenAsync(accessToken, cancellationToken);
  #endregion

  #region Write
  public async Task<Result<User?>> WriteUserAsync(User user, CancellationToken cancellationToken = default) {
    var upsert = await _userPersistence.UpsertUserAsync(user, cancellationToken);
    return upsert.IsSuccess
      ? Result<User?>.Ok(user)
      : upsert.ToResultOfType<User?>(null);
  }

  public Task<Result<User?>> CreateUserAsync(string username, string password, CancellationToken cancellationToken = default) =>
    _userPersistence.CreateUserWithPasswordAsync(username, password, _config.Pepper, cancellationToken);

  public async Task<Result<User?>> ChangePasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default) {
    var userResult = await _userPersistence.GetByIdAsync(userId, cancellationToken);
    if (!userResult.IsSuccess || userResult.Value == null)
      return userResult;

    var user = userResult.Value;
    var setPasswordResult = user.SetPassword(newPassword, _config.Pepper);
    if (!setPasswordResult.IsSuccess || setPasswordResult.Value == null)
      return setPasswordResult;

    var save = await _userPersistence.UpsertUserAsync(setPasswordResult.Value, cancellationToken);
    if (!save.IsSuccess)
      return save.ToResultOfType<User?>(null);

    return Result<User?>.Ok(setPasswordResult.Value);
  }
  #endregion

  #region Delete
  public Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
    _userPersistence.DeleteUserAsync(userId, cancellationToken);
  #endregion

  #region First boot Initialize
  public Task<Result> EnsureDefaultAdminAsync(CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(_defaultAdminBootstrap.Username) || string.IsNullOrWhiteSpace(_defaultAdminBootstrap.Password))
      return Task.FromResult(Result.BadRequest("Configure default admin: Configuration:CertsUIEngineConfiguration:Admin:Username and Admin:Password must be non-empty when bootstrapping the first user."));
    return _userPersistence.EnsureDefaultAdminAsync(_config.Pepper, _defaultAdminBootstrap.Username.Trim(), _defaultAdminBootstrap.Password, cancellationToken);
  }
  #endregion

  #region Login/Refresh/Logout
  public async Task<Result<LoginDomainResult?>> LoginAsync(string username, string password, string? twoFactorCode = null, string? twoFactorRecoveryCode = null, CancellationToken cancellationToken = default) {
    var usernameTrimmed = username?.Trim() ?? "";
    var passwordTrimmed = password?.Trim() ?? "";

    var userResult = await _userPersistence.GetByNameAsync(usernameTrimmed, cancellationToken);

    if (!userResult.IsSuccess || userResult.Value == null) {
      _logger.LogWarning("Login failed: user not found for username '{Username}' (trimmed: '{Trimmed}')", username ?? "(null)", usernameTrimmed);
      return Result<LoginDomainResult?>.Unauthorized(null, "Invalid username or password.");
    }

    var user = userResult.Value.RemoveRevokedTokens();

    if (!user.IsActive) {
      _logger.LogWarning("Login failed: user '{Username}' is not active", user.Username);
      return Result<LoginDomainResult?>.Unauthorized(null, "User is not active.");
    }

    if (string.IsNullOrWhiteSpace(_config.Pepper)) {
      _logger.LogWarning("Login failed: password pepper is not set (user '{Username}')", user.Username);
      return Result<LoginDomainResult?>.Unauthorized(null, "Invalid username or password.");
    }

    var validateHashResult = user.ValidatePassword(passwordTrimmed, _config.Pepper);
    if (!validateHashResult.IsSuccess) {
      _logger.LogWarning("Login failed: password validation failed for user '{Username}' (pepper length: {PepperLen}, password length: {PasswordLen})", user.Username, _config.Pepper?.Length ?? 0, passwordTrimmed.Length);
      return validateHashResult.ToResultOfType<LoginDomainResult?>(default);
    }

    if (user.TwoFactorEnabled) {
      if (string.IsNullOrWhiteSpace(twoFactorCode) && string.IsNullOrWhiteSpace(twoFactorRecoveryCode))
        return Result<LoginDomainResult?>.Unauthorized(null, "Two-factor authentication required.");

      Result validateResult = Result.Ok();

      if (!string.IsNullOrWhiteSpace(twoFactorCode))
        validateResult = ValidateTwoFactorCodeForUser(user, twoFactorCode);
      else if (!string.IsNullOrEmpty(twoFactorRecoveryCode))
        validateResult = user.ValidateRecoveryCode(twoFactorRecoveryCode);

      if (!validateResult.IsSuccess)
        return validateResult.ToResultOfType<LoginDomainResult?>(default);
    }

    user.SetLastLogin();

    var jwtResult = IssueJwtLoginDomainResult(user, replaceExisting: null);
    if (!jwtResult.IsSuccess || jwtResult.Value == null)
      return jwtResult;

    var saveResult = await _userPersistence.UpsertUserAsync(user, cancellationToken);
    if (!saveResult.IsSuccess)
      return saveResult.ToResultOfType<LoginDomainResult?>(default);

    return jwtResult;
  }

  public async Task<Result<LoginDomainResult?>> RefreshTokenAsync(string refreshToken, bool? force = false, CancellationToken cancellationToken = default) {
    var userResult = await _userPersistence.GetByRefreshTokenAsync(refreshToken, cancellationToken);
    if (!userResult.IsSuccess || userResult.Value == null)
      return Result<LoginDomainResult?>.Unauthorized(null, "Invalid refresh token.");

    var user = userResult.Value.RemoveRevokedTokens();

    var tokenDomain = user.Tokens.SingleOrDefault(t => t.RefreshToken == refreshToken);

    if (tokenDomain == null)
      return Result<LoginDomainResult?>.Unauthorized(null, "Invalid refresh token.");

    if (tokenDomain.IsRevoked)
      return Result<LoginDomainResult?>.Unauthorized(null, "Token has been revoked.");

    if (DateTime.UtcNow <= tokenDomain.ExpiresAt && force != true) {
      user.SetLastLogin();
      var saveRefresh = await _userPersistence.UpsertUserAsync(user, cancellationToken);
      if (!saveRefresh.IsSuccess)
        return saveRefresh.ToResultOfType<LoginDomainResult?>(default);

      return Result<LoginDomainResult?>.Ok(ToLoginResult(user, tokenDomain));
    }

    if (DateTime.UtcNow > tokenDomain.RefreshTokenExpiresAt) {
      user.RemoveToken(tokenDomain.Id);
      await _userPersistence.UpsertUserAsync(user, cancellationToken);
      return Result<LoginDomainResult?>.Unauthorized(null, "Refresh token has expired.");
    }

    user.SetLastLogin();

    var jwtResult = IssueJwtLoginDomainResult(user, tokenDomain);
    if (!jwtResult.IsSuccess || jwtResult.Value == null)
      return jwtResult;

    var saveRotated = await _userPersistence.UpsertUserAsync(user, cancellationToken);
    if (!saveRotated.IsSuccess)
      return saveRotated.ToResultOfType<LoginDomainResult?>(default);

    return jwtResult;
  }

  /// <summary>Logs out by access token; removes the session or all sessions.</summary>
  public async Task<Result> LogoutAsync(string accessToken, bool logoutFromAllDevices, CancellationToken cancellationToken = default) {
    var userResult = await _userPersistence.GetByAccessTokenAsync(accessToken, cancellationToken);
    if (userResult.IsSuccess && userResult.Value != null) {
      var user = userResult.Value;

      if (logoutFromAllDevices)
        user.RemoveAllTokens();
      else
        user.RemoveToken(accessToken);

      await _userPersistence.UpsertUserAsync(user, cancellationToken);
    }

    return Result.Ok();
  }
  #endregion

  #region Two-factor authentication
  public Result<List<string>?> EnableTwoFactorAuthForUser(User user) {
    if (user.TwoFactorEnabled)
      return Result<List<string>?>.BadRequest(null, "Two-factor authentication is already enabled.");

    if (string.IsNullOrWhiteSpace(_config.Pepper))
      return Result<List<string>?>.InternalServerError(null, "Password pepper is not configured.");

    if (!TotpGenerator.TryGenerateSecret(out string? secret, out string? errorMessage))
      return Result<List<string>?>.InternalServerError(null, errorMessage);

    user.SetTwoFactorSharedKey(secret);

    var clearRecoveryCodes = new List<string>(TwoFactorRecoveryCodeCount);
    List<TwoFactorRecoveryCode> recoveryCodeEntities = [];

    for (int i = 0; i < TwoFactorRecoveryCodeCount; i++) {
      var code = Guid.NewGuid().ToString("N")[..8];
      var formattedCode = $"{code[..4]}-{code[4..8]}";
      clearRecoveryCodes.Add(formattedCode);
    }

    foreach (var code in clearRecoveryCodes) {
      if (!PasswordHasher.TryCreateSaltedHash(code, _config.Pepper, out (string Salt, string Hash)? saltedHash, out string? hashError))
        return Result<List<string>?>.InternalServerError(null, hashError);

      var (salt, hash) = saltedHash.Value;
      recoveryCodeEntities.Add(new TwoFactorRecoveryCode(salt, _config.Pepper, hash));
    }

    user.SetTwoFactorRecoveryCodes(recoveryCodeEntities);
    return Result<List<string>?>.Ok(clearRecoveryCodes);
  }

  private Result ValidateTwoFactorCodeForUser(User user, string code) {
    if (string.IsNullOrWhiteSpace(code))
      return Result.BadRequest("Code is required.");

    if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSharedKey))
      return Result.BadRequest("Two-factor authentication is not enabled.");

    if (!TotpGenerator.TryValidate(code, user.TwoFactorSharedKey!, _twoFactorSettings.TimeTolerance, out bool isValid, out string? errorMessage))
      return Result.InternalServerError(errorMessage);

    return isValid ? Result.Ok() : Result.Unauthorized("Invalid two-factor code.");
  }
  #endregion

  #region JWT orchestration
  private Result<LoginDomainResult?> IssueJwtLoginDomainResult(User user, JwtToken? replaceExisting) {
    if (!JwtGenerator.TryGenerateToken(new JWTTokenGenerateRequest {
      Secret = _config.Secret,
      Issuer = _config.Issuer,
      Audience = _config.Audience,
      Expiration = _config.ExpirationMinutes,
      UserId = user.Id.ToString(),
      Username = user.Username,
    }, out (string token, JWTTokenClaims claims)? tokenData, out string? errorMessage)) {
      return Result<LoginDomainResult?>.InternalServerError(null, errorMessage);
    }

    var (tokenString, claims) = tokenData.Value;
    if (claims.IssuedAt == null || claims.ExpiresAt == null)
      return Result<LoginDomainResult?>.InternalServerError(null, "Invalid token claims: IssuedAt or ExpiresAt is null.");

    string refreshToken = JwtGenerator.GenerateRefreshToken();
    var refreshExpiresAt = claims.IssuedAt.Value.AddDays(_config.RefreshExpirationDays);

    JwtToken jwtToken = replaceExisting != null
      ? new JwtToken(replaceExisting.Id, tokenString, claims.IssuedAt.Value, claims.ExpiresAt.Value, refreshToken, refreshExpiresAt)
      : new JwtToken(tokenString, claims.IssuedAt.Value, claims.ExpiresAt.Value, refreshToken, refreshExpiresAt);

    user.RecordIssuedToken(jwtToken);
    return Result<LoginDomainResult?>.Ok(ToLoginResult(user, jwtToken));
  }

  private static LoginDomainResult ToLoginResult(User user, JwtToken tokenDomain) => new() {
    TokenType = tokenDomain.TokenType,
    Token = tokenDomain.Token,
    ExpiresAt = tokenDomain.ExpiresAt,
    RefreshToken = tokenDomain.RefreshToken,
    RefreshTokenExpiresAt = tokenDomain.RefreshTokenExpiresAt,
    Username = user.Username,
  };
  #endregion
}
