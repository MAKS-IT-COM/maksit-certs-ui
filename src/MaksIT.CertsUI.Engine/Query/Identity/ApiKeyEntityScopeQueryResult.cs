using MaksIT.Core.Abstractions.Query;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// One API key entity scope row for search/list results (includes parent API key description).
/// Certs uses <see cref="int"/> for type/scope (Vault uses engine enums) until a shared contract is referenced.
/// </summary>
public class ApiKeyEntityScopeQueryResult : QueryResultBase<Guid> {
  public required Guid ApiKeyId { get; set; }
  public string? Description { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public int EntityType { get; set; }
  public int Scope { get; set; }
}
