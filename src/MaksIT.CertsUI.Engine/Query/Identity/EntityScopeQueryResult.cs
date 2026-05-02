using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// One entity scope line in query results (organization or application scope with permissions).
/// </summary>
public class EntityScopeQueryResult {
  public ScopeEntityType ScopeEntityType { get; set; }
  public Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public bool Read { get; set; }
  public bool Write { get; set; }
  public bool Delete { get; set; }
  public bool Create { get; set; }
}

