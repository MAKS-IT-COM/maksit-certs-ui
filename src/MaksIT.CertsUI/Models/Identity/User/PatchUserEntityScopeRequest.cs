using MaksIT.CertsUI.Engine;
using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.User;

public class PatchUserEntityScopeRequest : PatchRequestModelBase {
  public Guid? Id { get; set; }

  /// <summary>
  /// The ID of the entity (e.g., organization or application). Optional for update-in-place when only Id is sent.
  /// </summary>
  public Guid? EntityId { get; set; }

  /// <summary>
  /// The type of the entity (e.g., "Organization", "Application"). Optional for update-in-place when only Id is sent.
  /// </summary>
  public ScopeEntityType? EntityType { get; set; }

  /// <summary>
  /// The scope granted to the user for this entity. Optional for update-in-place when only Id is sent.
  /// </summary>
  public ScopePermission? Scope { get; set; }
}