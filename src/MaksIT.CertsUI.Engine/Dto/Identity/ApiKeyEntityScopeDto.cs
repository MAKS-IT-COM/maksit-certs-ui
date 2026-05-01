namespace MaksIT.CertsUI.Engine.Dto.Identity;

/// <summary>
/// Placeholder row shape for API key entity scope queries (Vault parity). Not mapped to a table until scope storage exists; used for <see cref="QueryServices.Identity.IApiKeyEntityScopeQueryService"/> predicate typing.
/// </summary>
public sealed class ApiKeyEntityScopeDto {
  public Guid Id { get; set; }
  public Guid ApiKeyId { get; set; }
  public Guid EntityId { get; set; }
  public int EntityType { get; set; }
  public int Scope { get; set; }
}
