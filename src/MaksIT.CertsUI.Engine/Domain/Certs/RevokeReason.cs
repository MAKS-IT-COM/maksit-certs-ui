namespace MaksIT.CertsUI.Engine.Domain.Certs;

/// <summary>
/// ACME certificate revocation reason codes (RFC 5280 / ACME), passed to the CA when revoking a certificate.
/// </summary>
public enum RevokeReason {
  /// <summary>Unspecified.</summary>
  Unspecified = 0,
  /// <summary>Key compromise.</summary>
  KeyCompromise = 1,
  /// <summary>CA compromise.</summary>
  CaCompromise = 2,
  /// <summary>Affiliation changed.</summary>
  AffiliationChanged = 3,
  /// <summary>Superseded.</summary>
  Superseded = 4,
  /// <summary>Cessation of operation.</summary>
  CessationOfOperation = 5,
  /// <summary>Privilege withdrawn.</summary>
  PrivilegeWithdrawn = 6,
  /// <summary>AA compromise.</summary>
  AaCompromise = 7
}
