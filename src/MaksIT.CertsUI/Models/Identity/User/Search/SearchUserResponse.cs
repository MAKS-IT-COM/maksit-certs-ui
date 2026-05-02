using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.User.Search;

public class SearchUserResponse : ResponseModelBase {
  public required Guid Id { get; set; }

  #region Master data
  public required string Username { get; set; }
  public string? Email { get; set; }
  public string? MobileNumber { get; set; }
  public bool IsActive { get; set; }
  #endregion

  #region Two-factor authentication
  public bool TwoFactorEnabled { get; set; }
  public int? RecoveryCodesLeft { get; set; }
  #endregion

  public DateTime CreatedAt { get; set; }
  public DateTime? LastLogin { get; set; }

  #region Authorization
  public bool IsGlobalAdmin { get; set; }
  #endregion
}
