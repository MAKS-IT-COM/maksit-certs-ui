namespace MaksIT.LetsEncrypt.Entities.LetsEncrypt;

public enum RevokeReason {
  Unspecified = 0,
  KeyCompromise = 1,
  CaCompromise = 2,
  AffiliationChanged = 3,
  Superseded = 4,
  CessationOfOperation = 5,
  PrivilegeWithdrawn = 6,
  AaCompromise = 7
}