using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.CertsUI.Engine.Facades;


namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// Constructs a TwoFactorRecoveryCode from a DTO with the specified id, salt, hash, and isUsed status.
/// </summary>
/// <param name="id"></param>
/// <param name="salt"></param>
/// <param name="pepper"></param>
/// <param name="hash"></param>
/// <param name="isUsed"></param>
public class TwoFactorRecoveryCode(
  Guid id,
  string salt,
  string pepper,
  string hash,
  bool isUsed
) : DomainDocumentBase<Guid>(id) {

  /// <summary>
  /// Represents a recovery code for two-factor authentication (2FA).
  /// </summary>
  public string Salt { get; private set; } = salt;

  /// <summary>
  /// The hashed recovery code, generated using a secure hashing algorithm.
  /// </summary>
  public string Hash { get; private set; } = hash;

  /// <summary>
  /// A secret value used in the hashing process to enhance security. It should be kept confidential and not stored in the database.
  /// </summary>
  public string Pepper { get; private set; } = pepper;

  /// <summary>
  /// Indicates whether the recovery code has been used.
  /// </summary>
  public bool IsUsed { get; private set; } = isUsed;

  #region New entity constructor
  /// <summary>
  /// Constructs a new TwoFactorRecoveryCode with a new unique identifier, salt, and hash.
  /// </summary>
  /// <param name="salt"></param>
  /// <param name="hash"></param>
  public TwoFactorRecoveryCode(string salt, string pepper, string hash) : this(CombGui.GenerateCombGuid(), salt, pepper, hash, false) { }

  #endregion

  #region Fluent API for setting properties
  /// <summary>
  /// Sets the salt for this TwoFactorRecoveryCode instance.
  /// </summary>
  /// <param name="salt"></param>
  /// <returns></returns>
  public TwoFactorRecoveryCode SetSalt(string salt) {
    Salt = salt;
    return this;
  }

  /// <summary>
  /// Sets the hash for this TwoFactorRecoveryCode instance.
  /// </summary>
  /// <param name="hash"></param>
  /// <returns></returns>
  public TwoFactorRecoveryCode SetHash(string hash) {
    Hash = hash;
    return this;
  }

  /// <summary>
  /// Sets the used status for this TwoFactorRecoveryCode instance to true.
  /// </summary>
  /// <returns></returns>
  public TwoFactorRecoveryCode SetIsUsed() {
    IsUsed = true;
    return this;
  }

  /// <summary>
  /// Sets the used status for this TwoFactorRecoveryCode instance to the specified value.
  /// </summary>
  /// <param name="isUsed"></param>
  /// <returns></returns>
  public TwoFactorRecoveryCode SetIsUsed(bool isUsed) {
    IsUsed = isUsed;
    return this;
  }
  #endregion

  /// <summary>
  /// Validates the provided recovery code against the stored hash and salt.
  /// </summary>
  /// <param name="code"></param>
  /// <returns></returns>
  public bool ValidateCode(string code) {
    if (PasswordHasher.TryValidateHash(code, Salt, Hash, Pepper, out bool isValid, out string? errorMessage)) {
      return isValid;
    }

    return false;
  }
}
