using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Facades;


namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// User aggregate root: identity, credentials, and session tokens only.
/// JWT issuance and 2FA enable/validate are orchestrated by the identity domain service.
/// </summary>
public class User : DomainDocumentBase<Guid> {

  #region Master data Properties

  /// <summary>
  /// Represents the username of the user.
  /// </summary>
  public string Username { get; private set; }

  /// <summary>
  /// Represents the email address of the user.
  /// </summary>
  public string? Email { get; private set; }

  /// <summary>
  /// Represents the mobile number of the user.
  /// </summary>
  public string? MobileNumber { get; private set; }

  /// <summary>
  /// Indicates whether the user account is active or not.
  /// </summary>
  public bool IsActive { get; private set; }
  #endregion

  #region Authentication properties
  /// <summary>
  /// Represents the salt used for hashing the user's password.
  /// </summary>
  public string PasswordSalt { get; private set; }

  /// <summary>
  /// Represents the hashed password of the user.
  /// </summary>
  public string PasswordHash { get; private set; }

  /// <summary>
  /// Represents a list of JWT tokens associated with the user.
  /// </summary>
  public IReadOnlyList<JwtToken> Tokens => _tokens;
  private readonly List<JwtToken> _tokens = [];
  #endregion

  #region Two-factor authentication properties

  /// <summary>
  /// Indicates whether two-factor authentication is enabled for the user.
  /// </summary>
  public bool TwoFactorEnabled { get => TwoFactorSharedKey != null && _twoFactorRecoveryCodes.Count > 0; }

  /// <summary>
  /// Represents the shared key used for two-factor authentication, typically a TOTP secret.
  /// </summary>
  public string? TwoFactorSharedKey { get; private set; }

  /// <summary>
  /// Represents a list of recovery codes for two-factor authentication.
  /// </summary>
  public IReadOnlyList<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => _twoFactorRecoveryCodes;
  private readonly List<TwoFactorRecoveryCode> _twoFactorRecoveryCodes = [];
  #endregion

  /// <summary>
  /// Represents the date and time when the user was created.
  /// </summary>
  public DateTime CreatedAt { get; private set; }

  /// <summary>
  /// Represents the date and time when the user last logged in.
  /// </summary>
  public DateTime? LastLogin { get; private set; }

  #region New entity constructor
  /// <summary>
  /// Creates a new user (aggregate root). Use when registering a new identity.
  /// </summary>
  /// <param name="username">Non-empty username.</param>
  /// <param name="password">Non-empty password (will be hashed with pepper).</param>
  /// <param name="pepper">Pepper for password hashing (from config, not stored).</param>
  /// <exception cref="ArgumentNullException">If username, password, or pepper is null or whitespace.</exception>
  /// <exception cref="InvalidOperationException">If password hashing fails.</exception>
  public User(string username, string password, string pepper) : this(CombGui.GenerateCombGuid(), username, password, pepper) { }

  /// <summary>
  /// Creates a new user with an explicit Id (e.g. from a factory). Use when the Id is generated in one place to avoid duplicates.
  /// </summary>
  public User(Guid id, string username, string password, string pepper) : base(id) {
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    ArgumentException.ThrowIfNullOrWhiteSpace(password);
    ArgumentException.ThrowIfNullOrWhiteSpace(pepper);

    Username = username.Trim();

    if (!PasswordHasher.TryCreateSaltedHash(password, pepper, out (string PasswordSalt, string Hash)? saltedHash, out string? errorMessage)) {
      throw new InvalidOperationException(errorMessage);
    }

    (PasswordSalt, PasswordHash) = saltedHash.Value;
    CreatedAt = DateTime.UtcNow;
  }
  #endregion

  #region From DTO constructor
  /// <summary>
  /// Constructor for creating a User entity from a DTO, typically used when loading from a database or external source.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="username"></param>
  /// <param name="passwordSalt"></param>
  /// <param name="passwordHash"></param>
  /// <param name="createdAt"></param>
  public User(
    Guid id,
    string username,
    string passwordSalt,
    string passwordHash,
    DateTime createdAt
) : base(id) {
    Username = username;
    PasswordSalt = passwordSalt;
    PasswordHash = passwordHash;
    CreatedAt = createdAt;
  }
  #endregion

  #region Fluent API for setting properties

  // Master data Properties

  /// <summary>
  /// Sets the username for this user.
  /// </summary>
  /// <exception cref="ArgumentException">If username is null or whitespace.</exception>
  public User SetUsername(string username) {
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    Username = username.Trim();
    return this;
  }

  /// <summary>
  /// Sets the email address for this User instance.
  /// </summary>
  /// <param name="email"></param>
  /// <returns></returns>
  public User SetEmail(string? email) {
    Email = email;
    return this;
  }

  /// <summary>
  /// Sets the mobile number for this User instance.
  /// </summary>
  /// <param name="mobileNumber"></param>
  /// <returns></returns>
  public User SetMobileNumber(string? mobileNumber) {
    MobileNumber = mobileNumber;
    return this;
  }

  /// <summary>
  /// Sets the active status of the user.
  /// </summary>
  /// <param name="isActive"></param>
  /// <returns></returns>
  public User SetIsActive(bool isActive) {
    IsActive = isActive;
    return this;
  }



  // Authentication properties

  /// <summary>
  /// Sets the password for this user. Password is hashed with the provided pepper.
  /// </summary>
  /// <exception cref="ArgumentNullException">If newPassword or pepper is null or whitespace.</exception>
  /// <exception cref="InvalidOperationException">If hashing fails.</exception>
  public User SetPassword(string newPassword, string pepper) {
    ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
    ArgumentException.ThrowIfNullOrWhiteSpace(pepper);

    if (!PasswordHasher.TryCreateSaltedHash(newPassword, pepper, out (string Salt, string Hash)? saltedHash, out string? errorMessage)) {
      throw new InvalidOperationException(errorMessage);
    }

    (PasswordSalt, PasswordHash) = saltedHash.Value;
    return this;
  }

  /// <summary>
  /// Sets the JWT token for this User instance.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public User SetToken(JwtToken token) =>
    SetTokens([token]);

  public User SetTokens(List<JwtToken> tokens) {
    _tokens.Clear();
    _tokens.AddRange(tokens);
    return this;
  }

  /// <summary>
  /// Removes a JWT token from the user's token list by its ID.
  /// </summary>
  /// <param name="tokenId"></param>
  /// <returns></returns>
  public User RemoveToken(Guid tokenId) {
    _tokens.RemoveAll(x => x.Id == tokenId);
    return this;
  }

  /// <summary>
  /// Removes a JWT token from the user's token list by its token string.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public User RemoveToken(string token) {
    _tokens.RemoveAll(x => x.Token == token);
    return this;
  }

  /// <summary>
  /// Removes all revoked tokens from the user's token list.
  /// </summary>
  /// <returns></returns>
  public User RemoveRevokedTokens() {
    _tokens.RemoveAll(x => x.IsRevoked);
    return this;
  }

  /// <summary>
  /// Removes all tokens from the user's token list.
  /// </summary>
  /// <returns></returns>
  public User RemoveAllTokens() {
    _tokens.Clear();
    return this;
  }

  /// <summary>
  /// Upserts a JWT token in the user's token list. If the token already exists, it updates its properties; otherwise, it adds a new token.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public User UpsertToken(JwtToken token) =>
    UpsertTokens([token]);

  /// <summary>
  /// Upserts a list of JWT tokens in the user's token list. If a token already exists, it updates its properties; otherwise, it adds the new tokens.
  /// </summary>
  /// <param name="tokens"></param>
  /// <returns></returns>
  public User UpsertTokens(List<JwtToken> tokens) {
    foreach (var token in tokens) {
      var existingToken = _tokens.FirstOrDefault(x => x.Id == token.Id);

      if (existingToken != null) {
        existingToken
            .SetIsRevoked(token.IsRevoked)
            .SetToken(token.Token)
            .SetIssuedAt(token.IssuedAt)
            .SetExpiresAt(token.ExpiresAt)
            .SetRefreshToken(token.RefreshToken)
            .SetRefreshTokenExpiresAt(token.RefreshTokenExpiresAt);
      }
      else {
        _tokens.Add(token);
      }
    }
    return this;
  }


  // Two-factor authentication properties

  /// <summary>
  /// Sets the shared key for two-factor authentication (2FA).
  /// </summary>
  /// <param name="sharedKey"></param>
  /// <returns></returns>
  public User SetTwoFactorSharedKey(string? sharedKey) {
    TwoFactorSharedKey = sharedKey;
    return this;
  }

  /// <summary>
  /// Sets a single two-factor recovery code for the user.
  /// </summary>
  /// <param name="code"></param>
  /// <returns></returns>
  public User SetTwoFactorRecoveryCode(TwoFactorRecoveryCode code) =>
    SetTwoFactorRecoveryCodes([code]);

  /// <summary>
  /// Sets a list of two-factor recovery codes for the user.
  /// </summary>
  /// <param name="codes"></param>
  /// <returns></returns>
  public User SetTwoFactorRecoveryCodes(List<TwoFactorRecoveryCode> codes) {
    _twoFactorRecoveryCodes.Clear();
    _twoFactorRecoveryCodes.AddRange(codes);
    return this;
  }

  /// <summary>
  /// Removes a single two-factor recovery code by its ID.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public User RemoveTwoFactorRecoveryCode(Guid id) =>
    RemoveTwoFactorRecoveryCodes([id]);

  /// <summary>
  /// Removes multiple two-factor recovery codes by their IDs.
  /// </summary>
  /// <param name="ids"></param>
  /// <returns></returns>
  public User RemoveTwoFactorRecoveryCodes(List<Guid> ids) {
    _twoFactorRecoveryCodes.RemoveAll(x => ids.Contains(x.Id));
    return this;
  }

  /// <summary>
  /// Upserts a two-factor recovery code. If the code already exists (based on Salt and Hash), it updates it; otherwise, it adds a new code.
  /// </summary>
  /// <param name="code"></param>
  /// <returns></returns>
  public User UpsertTwoFactorRecoveryCode(TwoFactorRecoveryCode code) {
    _twoFactorRecoveryCodes.RemoveAll(x => x.Salt == code.Salt && x.Hash == code.Hash);
    _twoFactorRecoveryCodes.Add(code);
    return this;
  }

  /// <summary>
  /// Upserts a list of two-factor recovery codes. If a code already exists (based on Salt and Hash), it updates it; otherwise, it adds the new codes.
  /// </summary>
  /// <param name="codes"></param>
  /// <returns></returns>
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
  /// <returns></returns>
  public User SetLastLogin() {
    LastLogin = DateTime.UtcNow;
    return this;
  }

  /// <summary>
  /// Sets the last login time to a specific DateTime value.
  /// </summary>
  /// <param name="lastLogin"></param>
  /// <returns></returns>
  public User SetLastLogin(DateTime? lastLogin) {
    LastLogin = lastLogin;
    return this;
  }
  #endregion

  #region Methods
  /// <summary>
  /// Validates the provided password against the stored hash. Use the same pepper as at registration.
  /// </summary>
  /// <returns>Ok if valid; Unauthorized if password does not match.</returns>
  /// <exception cref="InvalidOperationException">If validation infrastructure fails.</exception>
  public Result ValidatePassword(string password, string pepper) {
    ArgumentException.ThrowIfNullOrWhiteSpace(password);
    ArgumentException.ThrowIfNullOrWhiteSpace(pepper);

    if (!PasswordHasher.TryValidateHash(password, PasswordSalt, PasswordHash, pepper, out bool isValid, out string? errorMessage)) {
      throw new InvalidOperationException(errorMessage);
    }

    return isValid ? Result.Ok() : Result.Unauthorized("Invalid password.");
  }

  /// <summary>
  /// Removes tokens whose refresh period has expired. Call before adding a new token to avoid unbounded growth.
  /// </summary>
  public User PruneExpiredRefreshTokens() {
    _tokens.RemoveAll(t => DateTime.UtcNow >= t.RefreshTokenExpiresAt);
    return this;
  }

  /// <summary>
  /// Records an issued JWT on this user (prunes expired refresh tokens then upserts the token). Keeps token-list invariants in the aggregate.
  /// </summary>
  public User RecordIssuedToken(JwtToken token) {
    PruneExpiredRefreshTokens();
    return UpsertToken(token);
  }

  /// <summary>
  /// Retrieves a JWT token by its ID.
  /// </summary>
  public Result<JwtToken?> GetToken(Guid tokenId) {
    var token = _tokens.FirstOrDefault(x => x.Id == tokenId);
    return token != null
        ? Result<JwtToken?>.Ok(token)
        : Result<JwtToken?>.NotFound(null, $"Token Id: {tokenId} not found");
  }

  /// <summary>
  /// Disables two-factor authentication by clearing the shared key and recovery codes.
  /// </summary>
  public Result<User?> DisableTwoFactorAuth() {
    TwoFactorSharedKey = null;
    _twoFactorRecoveryCodes.Clear();
    return Result<User?>.Ok(this);
  }

  /// <summary>
  /// Validates a one-time recovery code and marks it as used if valid.
  /// </summary>
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

  #endregion
}
