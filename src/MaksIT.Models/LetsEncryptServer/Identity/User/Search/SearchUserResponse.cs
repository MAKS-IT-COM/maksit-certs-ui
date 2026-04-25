using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.Identity.User.Search;

public class SearchUserResponse : ResponseModelBase {

  #region Master data Properties

  public required Guid Id { get; set; }
  public required string Username { get; set; }
  public bool IsActive { get; set; }
  public DateTime? LastLogin { get; set; }

  #endregion

  #region Two-factor authentication properties

  public bool TwoFactorEnabled { get; set; }

  #endregion
}
