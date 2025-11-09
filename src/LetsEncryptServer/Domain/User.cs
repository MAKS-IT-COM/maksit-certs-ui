using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.Results;

namespace MaksIT.LetsEncryptServer.Domain;

public class User(
  Guid id
) : DomainDocumentBase<Guid>(id) {
  public string Name { get; private set; } = string.Empty;
  public string Salt { get; private set; } = string.Empty;
  public string Hash { get; private set; } = string.Empty;
  public List<JwtToken> JwtTokens { get; private set; } = [];
  public DateTime LastLogin { get; private set; }

  public User() : this(Guid.NewGuid()) { }

  /// <summary>
  /// Change user name
  /// </summary>
  /// <param name="newName"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  public User SetName(string newName) {
    Name = newName;
    return this;
  }

  /// <summary>
  /// For persistence
  /// </summary>
  /// <param name="salt"></param>
  /// <param name="hash"></param>
  /// <returns></returns>
  public User SetSaltedHash(string salt, string hash) {
    Salt = salt;
    Hash = hash;

    return this;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="tokens"></param>
  /// <returns></returns>
  public User SetJwtTokens(List<JwtToken> tokens) {
    JwtTokens = tokens;
    return this;
  }

  public User SetLastLogin() {
    SetLastLogin(DateTime.UtcNow);
    return this;
  }

  public User SetLastLogin(DateTime dateTime) {
    LastLogin = dateTime;
    return this;
  }

  #region Password Management
  /// <summary>
  /// Set or change password (returns this for chaining)
  /// </summary>
  /// <param name="password"></param>
  /// <param name="pepper"></param>
  /// <returns></returns>
  public Result<User?> SetPassword(string password, string pepper) {
    if (!PasswordHasher.TryCreateSaltedHash(password, pepper, out var saltedHash, out var errorMessage))
      return Result<User?>.InternalServerError(null, errorMessage);

    Salt = saltedHash.Value.Salt;
    Hash = saltedHash.Value.Hash;

    return Result<User?>.Ok(this);
  }

  /// <summary>
  /// Validate password
  /// </summary>
  /// <param name="password"></param>
  /// <param name="pepper"></param>
  /// <returns></returns>
  public Result ValidatePassword(string password, string pepper) {
    if (PasswordHasher.TryValidateHash(password, Salt, Hash, pepper, out var isValid, out var errorMessage)) {
      if (isValid)
        return Result.Ok();

      return Result.Unauthorized("Invalid password.");
    }

    return Result<User?>.InternalServerError(null, errorMessage);
  }

  /// <summary>
  /// Reset password to a new value (returns this for chaining)
  /// </summary>
  /// <param name="newPassword"></param>
  /// <param name="pepper"></param>
  /// <returns></returns>
  public Result<User?> ResetPassword(string newPassword, string pepper) => SetPassword(newPassword, pepper);
  #endregion

  #region JWT Token Management
  public User UpsertJwtToken(JwtToken token) {
    var existing = JwtTokens.FirstOrDefault(t => t.Id == token.Id);

    if (existing != null)
      JwtTokens.Remove(existing);

    JwtTokens.Add(token);

    return this;
  }

  public User UpsertJwtTokens(List<JwtToken> tokens) {
    foreach (var token in tokens)
      UpsertJwtToken(token);

    return this;
  }

  public Result<User?> RemoveJwtToken(Guid tokenId) {
    var token = JwtTokens.FirstOrDefault(t => t.Id == tokenId);
    if (token == null)
      return Result<User?>.NotFound(null, "JWT token not found.");

    JwtTokens.Remove(token);
    return Result<User?>.Ok(this);
  }

  public Result<User?> RemoveJwtToken(string token) {
    var tokenDomain = JwtTokens.FirstOrDefault(t => t.Token == token);
    if (tokenDomain == null)
      return Result<User?>.NotFound(null, "JWT token not found.");
    JwtTokens.Remove(tokenDomain);
    return Result<User?>.Ok(this);
  }

  public Result<User?> RemoveJwtTokens(List<Guid> tokenIds) {

    foreach (var tokenId in tokenIds) {
      var removeTokenResult = RemoveJwtToken(tokenId);

      if (!removeTokenResult.IsSuccess)
        return removeTokenResult;
    }

    return Result<User?>.Ok(this);
  }

  public User RemoveRevokedJwtTokens() {
    JwtTokens = JwtTokens.Where(t => !t.IsRevoked).ToList();
    return this;
  }

  public User RevokeAllJwtTokens() {
    JwtTokens = [];
    return this;
  }

  #endregion
}
