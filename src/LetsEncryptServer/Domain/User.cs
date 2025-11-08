using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.Results;

namespace MaksIT.LetsEncryptServer.Domain;

public class User(
  Guid id,
  string name
  ) : DomainDocumentBase<Guid>(id) {
  public string Name { get; private set; } = name;
  public string Salt { get; private set; } = string.Empty;
  public string Hash { get; private set; } = string.Empty;

  public User(
    string name
  ) : this(
    Guid.NewGuid(),
    name
  ) { }


  // Set or change password (returns this for chaining)
  public Result<User?> SetPassword(string password, string pepper) {
    if (!PasswordHasher.TryCreateSaltedHash(password, pepper, out var saltedHash, out var errorMessage))
      return Result<User?>.InternalServerError(null, errorMessage);

    Salt = saltedHash.Value.Salt;
    Hash = saltedHash.Value.Hash;

    return Result<User?>.Ok(this);
  }

  // Reset password to a new value (returns this for chaining)
  public Result<User?> ResetPassword(string newPassword, string pepper) => SetPassword(newPassword, pepper);

  // Change user name
  public User ChangeName(string newName) {
    if (string.IsNullOrWhiteSpace(newName))
      throw new ArgumentException("Name cannot be empty.", nameof(newName));
    Name = newName;
    return this;
  }

  // Validate password
  public Result ValidatePassword(string password, string pepper) {
    if (PasswordHasher.TryValidateHash(password, Salt, Hash, pepper, out var isValid, out var errorMessage)) {
      if (isValid)
        return Result.Ok();

      return Result.Unauthorized("Invalid password.");
    }

    return Result<User?>.InternalServerError(null, errorMessage);
  }

  // For persistence
  public User SeltSaltedHash(string salt, string hash) {
    Salt = salt;
    Hash = hash;

    return this;
  }
}