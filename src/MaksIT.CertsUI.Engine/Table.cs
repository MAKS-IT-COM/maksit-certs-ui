using MaksIT.Core.Abstractions;

namespace MaksIT.CertsUI.Engine;

public class Table(int id, string name) : Enumeration(id, name) {

  #region Identity
  public static readonly Table Users = new(0, "users");
  public static readonly Table JwtTokens = new(3, "jwt_tokens");
  public static readonly Table TwoFactorRecoveryCodes = new(4, "two_factor_recovery_codes");
  #endregion

  #region ApiKeys
  public static readonly Table ApiKeys = new(1, "api_keys");
  #endregion

  #region Certs
  public static readonly Table RegistrationCaches = new(2, "registration_caches");
  #endregion
}
