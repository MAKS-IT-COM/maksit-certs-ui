using MaksIT.Core.Abstractions.Query;
using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// One user entity scope row for search/list results (includes parent user name).
/// </summary>
public class UserEntityScopeQueryResult : QueryResultBase<Guid> {
  public required Guid UserId { get; set; }
  public string? Username { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public ScopeEntityType EntityType { get; set; }
  public ScopePermission Scope { get; set; }
}
