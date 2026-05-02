using MaksIT.Core.Abstractions.Webapi;
using MaksIT.CertsUI.Engine;

namespace MaksIT.CertsUI.Models.APIKeys;

public class PatchApiKeyEntityScopeRequest : PatchRequestModelBase {
  public Guid? Id { get; set; }

  /// <summary>
  /// The ID of the entity (e.g., organization or application).
  /// </summary>
  public Guid EntityId { get; set; }

  /// <summary>
  /// The type of the entity (e.g., "Organization", "Application").
  /// </summary>
  public ScopeEntityType EntityType { get; set; }

  /// <summary>
  /// The scope granted to the user for this entity.
  /// </summary>
  public ScopePermission Scope { get; set; }
}
