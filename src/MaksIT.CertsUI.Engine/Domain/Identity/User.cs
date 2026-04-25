using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.CertsUI.Engine.Facades;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// User aggregate root: identity, credentials, and session tokens only. Passwords use per-user salt and application pepper via <see cref="PasswordHasher"/> (same pepper as API key material in <see cref="ApiKey"/>).
/// JWT issuance is orchestrated by the identity domain service.
/// </summary>
public class User : DomainDocumentBase<Guid> {

  #region Master data Properties

  /// <summary>
  /// Represents the username of the user (stored as <c>Name</c> in PostgreSQL).
  /// </summary>
  public string Username { get; private set; } = string.Empty;

  /// <summary>When false, the user cannot sign in.</summary>
  public bool IsActive { get; private set; } = true;

  #endregion

  #region Authentication properties

  /// <summary>
  /// Represents the salt used for hashing the user's password.
  /// </summary>
  public string PasswordSalt { get; private set; } = string.Empty;

  /// <summary>
  /// Represents the hashed password of the user.
  /// </summary>
  public string PasswordHash { get; private set; } = string.Empty;

  /// <summary>
  /// Represents a list of JWT tokens associated with the user.
  /// </summary>
  public IReadOnlyList<JwtToken> Tokens => _tokens;
  private readonly List<JwtToken> _tokens = [];

  #endregion

  #region Two-factor authentication properties

  /// <summary>True when a TOTP secret and at least one recovery code row exist.</summary>
  public bool TwoFactorEnabled => TwoFactorSharedKey != null && _twoFactorRecoveryCodes.Count > 0;

  /// <summary>TOTP shared secret (stored when 2FA is enabled).</summary>
  public string? TwoFactorSharedKey { get; private set; }

  public IReadOnlyList<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => _twoFactorRecoveryCodes;
  private readonly List<TwoFactorRecoveryCode> _twoFactorRecoveryCodes = [];

  #endregion

  /// <summary>
  /// Represents the date and time when the user last logged in (UTC).
  /// </summary>
  public DateTime? LastLogin { get; private set; }

  #region New entity constructor

  /// <summary>
  /// Creates a new user (aggregate root). Use when registering a new identity.
  /// </summary>
  /// <exception cref="InvalidOperationException">If password hashing fails.</exception>
  public User(string username, string password, string pepper) : this(CombGui.GenerateCombGuid(), username, password, pepper) { }

  /// <summary>
  /// Creates a new user with an explicit Id (e.g. from a factory).
  /// </summary>
  /// <exception cref="InvalidOperationException">If password hashing fails.</exception>
  public User(Guid id, string username, string password, string pepper) : base(id) {
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    ArgumentException.ThrowIfNullOrWhiteSpace(password);
    ArgumentException.ThrowIfNullOrWhiteSpace(pepper);

    Username = username.Trim();

    if (!PasswordHasher.TryCreateSaltedHash(password, pepper, out (string PasswordSalt, string Hash)? saltedHash, out string? errorMessage))
      throw new InvalidOperationException(errorMessage);

    (PasswordSalt, PasswordHash) = saltedHash.Value;
    IsActive = true;
  }

  #endregion

  #region From DTO constructor

  /// <summary>
  /// Constructor for creating a User entity when loading from persistence.
  /// </summary>
  public User(
    Guid id,
    string username,
    string passwordSalt,
    string passwordHash,
    DateTime? lastLogin,
    bool isActive,
    string? twoFactorSharedKey,
    IEnumerable<TwoFactorRecoveryCode>? twoFactorRecoveryCodes = null
  ) : base(id) {
    Username = username;
    PasswordSalt = passwordSalt;
    PasswordHash = passwordHash;
    LastLogin = lastLogin;
    IsActive = isActive;
    TwoFactorSharedKey = twoFactorSharedKey;
    if (twoFactorRecoveryCodes != null)
      _twoFactorRecoveryCodes.AddRange(twoFactorRecoveryCodes);
  }

  #endregion

  #region Fluent API for setting properties

  /// <summary>
  /// Sets the username for this user.
  /// </summary>
  public User SetUsername(string username) {
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    Username = username.Trim();
    return this;
  }

  /// <summary>Sets whether the account may sign in.</summary>
  public User SetIsActive(bool isActive) {
    IsActive = isActive;
    return this;
  }

  /// <summary>
  /// Sets the password for this user. Password is hashed with the provided pepper.
  /// </summary>
  public Result<User?> SetPassword(string newPassword, string pepper) {
    ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
    ArgumentException.ThrowIfNullOrWhiteSpace(pepper);

    if (!PasswordHasher.TryCreateSaltedHash(newPassword, pepper, out (string Salt, string Hash)? saltedHash, out string? errorMessage))
      return Result<User?>.InternalServerError(null, errorMessage);

    PasswordSalt = saltedHash.Value.Salt;
    PasswordHash = saltedHash.Value.Hash;
    return Result<User?>.Ok(this);
  }

  /// <summary>
  /// Replaces the token list (used when hydrating from storage).
  /// </summary>
  public User SetTokens(List<JwtToken> tokens) {
    _tokens.Clear();
    _tokens.AddRange(tokens);
    return this;
  }

  /// <summary>
  /// Sets a single JWT token (replaces list with one entry).
  /// </summary>
  public User SetToken(JwtToken token) =>
    SetTokens([token]);

  /// <summary>
  /// Removes a JWT token by its ID.
  /// </summary>
  public User RemoveToken(Guid tokenId) {
    _tokens.RemoveAll(x => x.Id == tokenId);
    return this;
  }

  /// <summary>
  /// Removes a JWT token by its access token string.
  /// </summary>
  public User RemoveToken(string token) {
    _tokens.RemoveAll(x => x.Token == token);
    return this;
  }

  /// <summary>
  /// Removes all revoked tokens from the user's token list.
  /// </summary>
  public User RemoveRevokedTokens() {
    _tokens.RemoveAll(x => x.IsRevoked);
    return this;
  }

  /// <summary>
  /// Removes all tokens from the user's token list.
  /// </summary>
  public User RemoveAllTokens() {
    _tokens.Clear();
    return this;
  }

  /// <summary>
  /// Upserts a JWT token (updates in place when the same <see cref="JwtToken.Id"/> exists).
  /// </summary>
  public User UpsertToken(JwtToken token) =>
    UpsertTokens([token]);

  /// <summary>
  /// Upserts multiple JWT tokens.
  /// </summary>
  public User UpsertTokens(List<JwtToken> tokens) {
    foreach (var t in tokens) {
      var existingToken = _tokens.FirstOrDefault(x => x.Id == t.Id);

      if (existingToken != null) {
        existingToken
          .SetIsRevoked(t.IsRevoked)
          .SetToken(t.Token)
          .SetIssuedAt(t.IssuedAt)
          .SetExpiresAt(t.ExpiresAt)
          .SetRefreshToken(t.RefreshToken)
          .SetRefreshTokenExpiresAt(t.RefreshTokenExpiresAt);
      }
      else {
        _tokens.Add(t);
      }
    }
    return this;
  }

  /// <summary>Sets the TOTP shared secret (or clears when disabling 2FA).</summary>
  public User SetTwoFactorSharedKey(string? sharedKey) {
    TwoFactorSharedKey = sharedKey;
    return this;
  }

  /// <summary>Replaces recovery codes (used after enable or when loading from persistence).</summary>
  public User SetTwoFactorRecoveryCodes(List<TwoFactorRecoveryCode> codes) {
    _twoFactorRecoveryCodes.Clear();
    _twoFactorRecoveryCodes.AddRange(codes);
    return this;
  }

  /// <summary>
  /// Upserts recovery codes by Id (matches Vault semantics for persistence sync).
  /// </summary>
  public User UpsertTwoFactorRecoveryCodes(List<TwoFactorRecoveryCode> codes) {
    foreach (var code in codes) {
      var existingCode = _twoFactorRecoveryCodes.FirstOrDefault(x => x.Id == code.Id);

      if (existingCode != null) {
        existingCode
          .SetSalt(code.Salt)
          .SetHash(code.Hash)
          .SetIsUsed(code.IsUsed);
      }
      else {
        _twoFactorRecoveryCodes.Add(code);
      }
    }
    return this;
  }

  /// <summary>
  /// Sets the last login time to the current UTC time.
  /// </summary>
  public User SetLastLogin() {
    LastLogin = DateTime.UtcNow;
    return this;
  }

  /// <summary>
  /// Sets the last login time to a specific instant (UTC).
  /// </summary>
  public User SetLastLogin(DateTime? lastLogin) {
    LastLogin = lastLogin;
    return this;
  }

  #endregion

  #region Methods

  /// <summary>
  /// Validates the provided password against the stored hash.
  /// </summary>
  public Result ValidatePassword(string password, string pepper) {
    if (PasswordHasher.TryValidateHash(password, PasswordSalt, PasswordHash, pepper, out var isValid, out var errorMessage)) {
      if (isValid)
        return Result.Ok();
      return Result.Unauthorized("Invalid password.");
    }

    return Result.InternalServerError([errorMessage ?? "Password validation failed."]);
  }

  /// <summary>Clears TOTP secret and recovery codes.</summary>
  public Result<User?> DisableTwoFactorAuth() {
    TwoFactorSharedKey = null;
    _twoFactorRecoveryCodes.Clear();
    return Result<User?>.Ok(this);
  }

  /// <summary>Validates a one-time recovery code and marks it used when valid.</summary>
  public Result ValidateRecoveryCode(string code) {
    if (string.IsNullOrWhiteSpace(code))
      return Result.BadRequest("Recovery code is required.");

    var recoveryCode = _twoFactorRecoveryCodes.FirstOrDefault(rc => rc.ValidateCode(code));
    if (recoveryCode == null)
      return Result.NotFound("Invalid recovery code.");
    if (recoveryCode.IsUsed)
      return Result.Unauthorized("Two-factor recovery code is already used.");

    recoveryCode.SetIsUsed();
    return Result.Ok();
  }

  /// <summary>
  /// Removes tokens whose refresh period has expired.
  /// </summary>
  public User PruneExpiredRefreshTokens() {
    _tokens.RemoveAll(t => DateTime.UtcNow >= t.RefreshTokenExpiresAt);
    return this;
  }

  /// <summary>
  /// Records an issued JWT (prunes expired refresh tokens, then upserts).
  /// </summary>
  public User RecordIssuedToken(JwtToken token) {
    PruneExpiredRefreshTokens();
    return UpsertToken(token);
  }

  public Result<JwtToken?> GetToken(Guid tokenId) {
    var token = _tokens.FirstOrDefault(x => x.Id == tokenId);
    return token != null
      ? Result<JwtToken?>.Ok(token)
      : Result<JwtToken?>.NotFound(null, $"Token Id: {tokenId} not found");
  }

  public Result<User?> ResetPassword(string newPassword, string pepper) => SetPassword(newPassword, pepper);

  #endregion
}
