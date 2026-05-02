using MaksIT.Core.Abstractions.Webapi;
using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI.Models.APIKeys.Search;

/// <summary>
/// One API key entity scope row in search results (includes parent API key description).
/// </summary>
public class SearchApiKeyEntityScopeResponse : ResponseModelBase {
  public required Guid Id { get; set; }
  public required Guid ApiKeyId { get; set; }
  public string? Description { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public ScopeEntityType EntityType { get; set; }
  public ScopePermission Scope { get; set; }
}
