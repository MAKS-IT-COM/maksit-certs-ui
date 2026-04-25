using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security;
using MaksIT.CertsUI.Engine.Facades;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// Hashed two-factor recovery code (RFC-style one-time backup codes). Clear values are only returned once when 2FA is enabled.
/// </summary>
public class TwoFactorRecoveryCode(
  Guid id,
  string salt,
  string pepper,
  string hash,
  bool isUsed
) : DomainDocumentBase<Guid>(id) {

  public string Salt { get; private set; } = salt;
  public string Hash { get; private set; } = hash;
  public string Pepper { get; private set; } = pepper;
  public bool IsUsed { get; private set; } = isUsed;

  #region New entity constructor

  public TwoFactorRecoveryCode(string salt, string pepper, string hash) : this(CombGui.GenerateCombGuid(), salt, pepper, hash, false) { }

  #endregion

  #region Fluent API

  public TwoFactorRecoveryCode SetSalt(string salt) {
    Salt = salt;
    return this;
  }

  public TwoFactorRecoveryCode SetHash(string hash) {
    Hash = hash;
    return this;
  }

  public TwoFactorRecoveryCode SetIsUsed() {
    IsUsed = true;
    return this;
  }

  public TwoFactorRecoveryCode SetIsUsed(bool isUsed) {
    IsUsed = isUsed;
    return this;
  }

  #endregion

  public bool ValidateCode(string code) {
    if (PasswordHasher.TryValidateHash(code, Salt, Hash, Pepper, out bool isValid, out _))
      return isValid;
    return false;
  }
}
