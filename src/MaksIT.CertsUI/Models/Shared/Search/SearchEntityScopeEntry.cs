using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI.Models.Shared.Search;

/// <summary>
/// One entity scope line in search results (organization or application scope with permissions).
/// </summary>
public class SearchEntityScopeEntry {
  public ScopeEntityType ScopeEntityType { get; set; }
  public Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public bool Read { get; set; }
  public bool Write { get; set; }
  public bool Delete { get; set; }
  public bool Create { get; set; }
}
