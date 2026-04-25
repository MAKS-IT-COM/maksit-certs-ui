using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Identity.User;

public class UserResponse : ResponseModelBase {

  #region Master data Properties

  public Guid Id { get; set; }
  public string? Username { get; set; }
  public bool IsActive { get; set; }
  public DateTime? LastLogin { get; set; }

  #endregion

  #region Two-factor authentication properties

  public bool TwoFactorEnabled { get; set; }
  public List<string>? TwoFactorRecoveryCodes { get; set; }
  public string? QrCodeUrl { get; set; }
  public int? RecoveryCodesLeft { get; set; }

  #endregion
}
