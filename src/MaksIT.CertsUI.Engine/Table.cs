using MaksIT.Core.Abstractions;

namespace MaksIT.CertsUI.Engine;

public class Table(int id, string name) : Enumeration(id, name) {

  #region Identity
  public static readonly Table Users = new(0, "users");
  public static readonly Table JwtTokens = new(1, "jwt_tokens");
  public static readonly Table TwoFactorRecoveryCodes = new(2, "two_factor_recovery_codes");
  #endregion

  #region ApiKeys
  public static readonly Table ApiKeys = new(3, "api_keys");
  #endregion

  #region Scope permissions management
    
  public static readonly Table UserEntityScopes = new(4, "user_entity_scopes");
  public static readonly Table ApiKeyEntityScopes = new(5, "api_key_entity_scopes");
  #endregion

  #region Certs
  public static readonly Table RegistrationCaches = new(6, "registration_caches");
  public static readonly Table TermsOfServiceCache = new(7, "terms_of_service_cache");
  public static readonly Table AcmeSessions = new(8, "acme_sessions");
  #endregion
}
