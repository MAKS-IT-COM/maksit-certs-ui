using MaksIT.Core.Abstractions.Query;
using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// One API key entity scope row for search/list results (includes parent API key description).
/// </summary>
public class ApiKeyEntityScopeQueryResult : QueryResultBase<Guid> {
  public required Guid ApiKeyId { get; set; }
  public string? Description { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public ScopeEntityType EntityType { get; set; }
  public ScopePermission Scope { get; set; }
}
