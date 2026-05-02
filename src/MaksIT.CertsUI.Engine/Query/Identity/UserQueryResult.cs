using MaksIT.Core.Abstractions.Query;
using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Engine.Query.Identity;

public class UserQueryResult : QueryResultBase<Guid> {
  #region Master data Properties
  public required string Username { get; set; }
  public string? Email { get; set; }
  public string? MobileNumber { get; set; }
  public bool IsActive { get; set; }
  #endregion

  #region Two-factor authentication properties
  public bool TwoFactorEnabled { get; set; }
  public int? RecoveryCodesLeft { get; set; }
  #endregion

  public DateTime CreatedAt { get; set; }
  public DateTime? LastLogin { get; set; }

  #region Authorization management
  public bool IsGlobalAdmin { get; set; }
  #endregion
}
