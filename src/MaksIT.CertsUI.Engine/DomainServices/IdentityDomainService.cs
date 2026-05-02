using MaksIT.Results;
using MaksIT.Core.Security;
using MaksIT.Core.Security.JWT;
using MaksIT.CertsUI.Engine.Domain.Identity;
using Microsoft.Extensions.Logging;
using MaksIT.CertsUI.Engine.Persistence.Services;


namespace MaksIT.CertsUI.Engine.DomainServices;

public interface IIdentityDomainService {

  #region Read
  Result<User?> ReadUserById(Guid userId);
  Result<User?> ReadUserByUsername(string usenrame);
  Result<UserAuthorization?> ReadUserAuthorization(Guid userId);
  #endregion

  #region First boot Initialize
  Task<Result<User?>> InitializeAdminAsync();
  #endregion

  #region Write
  Task<Result<User?>> WriteUserAsync(User user);
  /// <summary>Persists user with the given authorization. When null, current authorization is preserved.</summary>
  Task<Result<User?>> WriteUserAsync(User user, UserAuthorization? authorization);
  Task<Result> WriteUserAuthorizationAsync(UserAuthorization authorization);
  #endregion

  #region Delete
  Task<Result> DeleteUserAsync(Guid userId);
  #endregion

  #region Login/Refresh/Logout
  Task<Result<JwtToken?>> LoginAsync(string username, string password, string? twoFactorCode = null, string? twoFactorRecoveryCode = null);
  Task<Result<JwtToken?>> RefreshTokenAsync(string refreshToken, bool? force = false);
  Task<Result> LogoutAsync(Guid userId, string accessToken, bool allDevices = false);
  #endregion

  /// <summary>
  /// Enables two-factor authentication for the user: generates shared key and recovery codes, mutates the user.
  /// </summary>
  /// <returns>Plain recovery codes to show the user once; caller must persist the user after.</returns>
  Result<List<string>?> EnableTwoFactorAuthForUser(User user);
}

public class IdentityDomainService(
  ILogger<IdentityDomainService> logger,
  IIdentityPersistenceService identityPersistenceService,
  IUserAuthorizationPersistenceService userAuthorizationPersistenceService,
  ICertsEngineConfiguration vaultEngineConfiguration,
  IAdminUser adminUser,
  IJwtSettingsConfiguration jwtSettingsConfiguration,
  ITwoFactorSettingsConfiguration twoFactorSettingsConfiguration
) : IIdentityDomainService {

  private readonly ILogger<IdentityDomainService> _logger = logger;
  private readonly IIdentityPersistenceService _identityPersistenceService = identityPersistenceService;
  private readonly IUserAuthorizationPersistenceService _userAuthorizationPersistenceService = userAuthorizationPersistenceService;
  private readonly ICertsEngineConfiguration _vaultEngineConfiguration = vaultEngineConfiguration;
  private readonly IAdminUser _adminUser = adminUser;
  private readonly IJwtSettingsConfiguration _jwtSettingsConfiguration = jwtSettingsConfiguration;
  private readonly ITwoFactorSettingsConfiguration _twoFactorSettingsConfiguration = twoFactorSettingsConfiguration;

  #region Read
  public Result<User?> ReadUserById(Guid userId) =>
  _identityPersistenceService.ReadById(userId);

  public Result<User?> ReadUserByUsername(string usenrame) =>
    _identityPersistenceService.ReadByUsername(usenrame);

  public Result<UserAuthorization?> ReadUserAuthorization(Guid userId) =>
    _userAuthorizationPersistenceService.ReadByUserId(userId);
  #endregion

  #region First boot Initialize
  public async Task<Result<User?>> InitializeAdminAsync() {
    var adminUserIdsResult = _userAuthorizationPersistenceService.ReadGlobalAdminUserIds();
    if (adminUserIdsResult.IsSuccess && adminUserIdsResult.Value != null && adminUserIdsResult.Value.Count > 0) {
      var firstId = adminUserIdsResult.Value[0];
      var existing = _identityPersistenceService.ReadById(firstId);
      if (existing.IsSuccess && existing.Value != null)
        return existing;
      return Result<User?>.BadRequest(null, "Global admin is referenced but the user record is missing.");
    }

    // Use the same pepper as login so hashed password matches validation; require it to be set (e.g. from appsecrets.json).
    var pepper = _vaultEngineConfiguration.JwtSettingsConfiguration?.PasswordPepper;
    if (string.IsNullOrWhiteSpace(pepper)) {
      return Result<User?>.BadRequest(null, "PasswordPepper is not set. Set Configuration:VaultEngineConfiguration:JwtSettingsConfiguration:PasswordPepper in appsecrets.json (or config) so bootstrap and login use the same pepper.");
    }

    var usernameTrimmed = (_adminUser.Username ?? "").Trim();
    var passwordTrimmed = (_adminUser.Password ?? "").Trim();
    if (string.IsNullOrWhiteSpace(usernameTrimmed) || string.IsNullOrWhiteSpace(passwordTrimmed)) {
      return Result<User?>.BadRequest(null, "Admin Username and Password must be set in configuration.");
    }

    var user = new User(usernameTrimmed, passwordTrimmed, pepper!)
      .SetIsActive(true);

    var authorization = new UserAuthorization(user.Id).SetIsGlobalAdmin(true);
    var upsertResult = _identityPersistenceService.Write(user, authorization);
    if (!upsertResult.IsSuccess || upsertResult.Value == null)
      return upsertResult.ToResultOfType<User?>(_ => null);

    return upsertResult;
  }
  #endregion

  #region Write
  /// <summary>Persists user; loads current authorization from DB so auth is not overwritten.</summary>
  public Task<Result<User?>> WriteUserAsync(User user) =>
    WriteUserAsync(user, null);

  /// <summary>Persists user and optional authorization. When authorization is null, loads current authorization from DB so auth is not overwritten.</summary>
  public Task<Result<User?>> WriteUserAsync(User user, UserAuthorization? authorization) {
    var authToApply = authorization;
    if (authToApply == null) {
      var authResult = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
      authToApply = authResult.IsSuccess ? authResult.Value : null;
    }
    return Task.FromResult(_identityPersistenceService.Write(user, authToApply));
  }

  public Task<Result> WriteUserAuthorizationAsync(UserAuthorization authorization) =>
    Task.FromResult(_userAuthorizationPersistenceService.Write(authorization));
  #endregion

  #region Delete
  public Task<Result> DeleteUserAsync(Guid userId) =>
    Task.FromResult(_identityPersistenceService.DeleteById(userId));
  #endregion

  #region Login/Refresh/Logout
  public async Task<Result<JwtToken?>> LoginAsync(string username, string password, string? twoFactorCode = null, string? twoFactorRecoveryCode = null) {
    var usernameTrimmed = username?.Trim() ?? "";
    var passwordTrimmed = password?.Trim() ?? "";

    var userResult = _identityPersistenceService.ReadByUsername(usernameTrimmed);

    if(!userResult.IsSuccess || userResult.Value == null) {
      _logger.LogWarning("Login failed: user not found for username '{Username}' (trimmed: '{Trimmed}')", username ?? "(null)", usernameTrimmed);
      return Result<JwtToken?>.Unauthorized(null, "Invalid username or password.");
    }

    var user = userResult.Value.RemoveRevokedTokens();

    if(!user.IsActive) {
      _logger.LogWarning("Login failed: user '{Username}' is not active", user.Username);
      return Result<JwtToken?>.Unauthorized(null, "User is not active.");
    }

    var pepper = _vaultEngineConfiguration.JwtSettingsConfiguration?.PasswordPepper;
    if (string.IsNullOrWhiteSpace(pepper)) {
      _logger.LogWarning("Login failed: PasswordPepper is not set (user '{Username}')", user.Username);
      return Result<JwtToken?>.Unauthorized(null, "Invalid username or password.");
    }

    var validateHashResult = user.ValidatePassword(passwordTrimmed, pepper);

    if (!validateHashResult.IsSuccess) {
      _logger.LogWarning("Login failed: password validation failed for user '{Username}' (pepper length: {PepperLen}, password length: {PasswordLen})", user.Username, pepper?.Length ?? 0, passwordTrimmed.Length);
      return Result<JwtToken?>.Unauthorized(null, "Invalid username or password.");
    }

    if (user.TwoFactorEnabled) {
      if (string.IsNullOrWhiteSpace(twoFactorCode) && string.IsNullOrWhiteSpace(twoFactorRecoveryCode))
        return Result<JwtToken?>.Unauthorized(null, "Two-factor authentication required.");

      Result validateResult = Result.Ok();

      if (!string.IsNullOrWhiteSpace(twoFactorCode))
        validateResult = ValidateTwoFactorCodeForUser(user, twoFactorCode);
      else if (!string.IsNullOrEmpty(twoFactorRecoveryCode))
        validateResult = user.ValidateRecoveryCode(twoFactorRecoveryCode);

      if (!validateResult.IsSuccess)
        return validateResult.ToResultOfType<JwtToken?>((JwtToken?)null);
    }

    user.SetLastLogin();

    var authResult = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
    var aclEntries = authResult.IsSuccess && authResult.Value != null
      ? authResult.Value.GetAclEntries()
      : [];

    var jwtTokenResult = IssueJwtForUser(user, aclEntries);
    if (!jwtTokenResult.IsSuccess || jwtTokenResult.Value == null)
      return jwtTokenResult;

    var writeUserResult = _identityPersistenceService.Write(user, authResult.IsSuccess ? authResult.Value : null);
    if (!writeUserResult.IsSuccess || writeUserResult.Value == null)
      return writeUserResult.ToResultOfType<JwtToken?>(_ => null);

    return jwtTokenResult;
  }

  public async Task<Result<JwtToken?>> RefreshTokenAsync(string refreshToken, bool? force = false) {
    var userResult = _identityPersistenceService.ReadByRefreshToken(refreshToken);
    if (!userResult.IsSuccess || userResult.Value == null)
      return Result<JwtToken?>.Unauthorized(null, "Invalid refresh token.");

    var user = userResult.Value.RemoveRevokedTokens();

    var token = user.Tokens.SingleOrDefault(token => token.RefreshToken == refreshToken);

    if (token == null)
      return Result<JwtToken?>.Unauthorized(null, "Invalid refresh token.");

    if (token.IsRevoked)
      return Result<JwtToken?>.Unauthorized(null, "Token has been revoked.");

    Result<User?>? writeResult = default;

    // If refresh token is still valid and force is not set, do nothing, just return the same token
    if (DateTime.UtcNow <= token.ExpiresAt && force != true) {
      user.SetLastLogin();

      // Update the user status in the database
      var authForRefresh = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
      writeResult = _identityPersistenceService.Write(user, authForRefresh.IsSuccess ? authForRefresh.Value : null);
      if (!writeResult.IsSuccess || writeResult.Value == null)
        return writeResult.ToResultOfType<JwtToken?>(_ => null);

      return Result<JwtToken?>.Ok(token);
    }

    // If refresh token is expired, it cannot be used to get a new token; remove it and persist.
    if (DateTime.UtcNow > token.RefreshTokenExpiresAt) {
      user.RemoveToken(token.Id);
      var authForRemove = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
      _identityPersistenceService.Write(user, authForRemove.IsSuccess ? authForRemove.Value : null);
      return Result<JwtToken?>.Unauthorized(null, "Refresh token has expired.");
    }

    user.SetLastLogin();

    var authResult = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
    var aclEntries = authResult.IsSuccess && authResult.Value != null
      ? authResult.Value.GetAclEntries()
      : [];

    var jwtTokenResult = IssueJwtForUser(user, aclEntries);
    if (!jwtTokenResult.IsSuccess || jwtTokenResult.Value == null)
      return jwtTokenResult;

    // Update the user status in the database
    writeResult = _identityPersistenceService.Write(user, authResult.IsSuccess ? authResult.Value : null);
    if (!writeResult.IsSuccess || writeResult.Value == null)
      return writeResult.ToResultOfType<JwtToken?>(_ => null);

    return jwtTokenResult;
  }

  /// <summary>Logs out by user id and access token from the JWT payload; removes the session or all sessions.</summary>
  public async Task<Result> LogoutAsync(Guid userId, string accessToken, bool allDevices = false) {
    var userResult = _identityPersistenceService.ReadById(userId);
    if (!userResult.IsSuccess || userResult.Value == null)
      return Result.Ok();

    var user = userResult.Value;

    if (allDevices)
      user.RemoveAllTokens();
    else
      user.RemoveToken(accessToken);

    var authResult = _userAuthorizationPersistenceService.ReadByUserId(user.Id);
    _identityPersistenceService.Write(user, authResult.IsSuccess ? authResult.Value : null);
    return Result.Ok();
  }
  #endregion

  #region Two-factor and JWT orchestration
  private const int TwoFactorRecoveryCodeCount = 5;

  public Result<List<string>?> EnableTwoFactorAuthForUser(User user) {
    if (user.TwoFactorEnabled)
      return Result<List<string>?>.BadRequest(null, "Two-factor authentication is already enabled.");

    var pepper = _vaultEngineConfiguration.JwtSettingsConfiguration.PasswordPepper;
    if (string.IsNullOrWhiteSpace(pepper))
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
      if (!PasswordHasher.TryCreateSaltedHash(code, pepper, out (string Salt, string Hash)? saltedHash, out string? hashError))
        return Result<List<string>?>.InternalServerError(null, hashError);

      var (salt, hash) = saltedHash.Value;
      recoveryCodeEntities.Add(new TwoFactorRecoveryCode(salt, pepper, hash));
    }

    user.SetTwoFactorRecoveryCodes(recoveryCodeEntities);
    return Result<List<string>?>.Ok(clearRecoveryCodes);
  }

  private Result ValidateTwoFactorCodeForUser(User user, string code) {
    if (string.IsNullOrWhiteSpace(code))
      return Result.BadRequest("Code is required.");

    if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSharedKey))
      return Result.BadRequest("Two-factor authentication is not enabled.");

    if (!TotpGenerator.TryValidate(code, user.TwoFactorSharedKey!, _twoFactorSettingsConfiguration.TimeTolerance, out bool isValid, out string? errorMessage))
      return Result.InternalServerError(errorMessage);

    return isValid ? Result.Ok() : Result.Unauthorized("Invalid two-factor code.");
  }

  private Result<JwtToken?> IssueJwtForUser(User user, IReadOnlyList<string> aclEntries) {
    if (!JwtGenerator.TryGenerateToken(new JWTTokenGenerateRequest {
      Secret = _jwtSettingsConfiguration.JwtSecret,
      Issuer = _jwtSettingsConfiguration.Issuer,
      Audience = _jwtSettingsConfiguration.Audience,
      Expiration = _jwtSettingsConfiguration.ExpiresIn,
      UserId = user.Id.ToString(),
      Username = user.Username,
      AclEntries = aclEntries?.ToList() ?? []
    }, out (string token, JWTTokenClaims claims)? tokenData, out string? errorMessage)) {
      return Result<JwtToken?>.InternalServerError(null, errorMessage);
    }

    var (tokenString, claims) = tokenData.Value;
    if (claims.IssuedAt == null || claims.ExpiresAt == null)
      return Result<JwtToken?>.InternalServerError(null, "Invalid token claims: IssuedAt or ExpiresAt is null.");

    string refreshToken = JwtGenerator.GenerateRefreshToken();
    var jwtToken = new JwtToken(
      tokenString,
      claims.IssuedAt.Value,
      claims.ExpiresAt.Value,
      refreshToken,
      claims.IssuedAt.Value.AddDays(_jwtSettingsConfiguration.RefreshTokenExpiresIn)
    );

    user.RecordIssuedToken(jwtToken);
    return Result<JwtToken?>.Ok(jwtToken);
  }
  #endregion
}
