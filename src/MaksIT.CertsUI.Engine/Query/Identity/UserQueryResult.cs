using MaksIT.Core.Abstractions.Query;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// Projection for user list/search rows.
/// </summary>
public class UserQueryResult : QueryResultBase<Guid> {
  public required string Username { get; set; }
  public bool IsActive { get; set; }
  public bool TwoFactorEnabled { get; set; }
  public DateTime? LastLogin { get; set; }
}
