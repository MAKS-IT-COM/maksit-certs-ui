using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Identity.User;

public class PatchUserRequest : PatchRequestModelBase {

  #region Master data Properties

  public bool? IsActive { get; set; }

  #endregion

  #region Authentication properties

  public string? Password { get; set; }

  #endregion

  #region Two-factor authentication properties

  public bool? TwoFactorEnabled { get; set; }

  #endregion
}
