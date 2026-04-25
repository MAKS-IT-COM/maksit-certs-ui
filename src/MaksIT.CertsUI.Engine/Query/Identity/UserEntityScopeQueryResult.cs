using MaksIT.Core.Abstractions.Query;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// One user entity scope row for search/list results (includes parent user name).
/// Certs uses <see cref="int"/> for type/scope until shared enums are referenced.
/// </summary>
public class UserEntityScopeQueryResult : QueryResultBase<Guid> {
  public required Guid UserId { get; set; }
  public string? Username { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public int EntityType { get; set; }
  public int Scope { get; set; }
}
