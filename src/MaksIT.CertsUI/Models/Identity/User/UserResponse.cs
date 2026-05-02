using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.User;

public class UserResponse : ResponseModelBase {
  #region Master data Properties
  public required Guid Id { get; set; }
  public required string Username { get; set; }
  public string? Email { get; set; }
  public string? MobileNumber { get; set; }
  public bool IsActive { get; set; }
  #endregion

  #region Two-factor authentication properties
  public bool TwoFactorEnabled { get; set; }
  public List<string>? TwoFactorRecoveryCodes { get; set; }
  public string? QrCodeUrl { get; set; }
  public int? RecoveryCodesLeft { get; set; }
  #endregion

  #region Authorization properties
  public bool IsGlobalAdmin { get; set; }
  public List<UserEntityScopeResponse> EntityScopes { get; set; } = [];
  #endregion
}
