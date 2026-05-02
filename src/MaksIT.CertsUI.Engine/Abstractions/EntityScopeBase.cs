using MaksIT.Core.Abstractions.Domain;
using MaksIT.CertsUI.Engine.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.CertsUI.Engine.Abstractions;

/// <summary>
/// Base class for per-entity scopes. Hosts standardized JWT helpers.
/// </summary>
public abstract class EntityScopeBase : DomainDocumentBase<Guid> {

  /// <summary>
  /// Gets the unique identifier of the entity (for example, OrganizationId or ApplicationId).
  /// </summary>
  public Guid EntityId { get; protected set; }

  /// <summary>
  /// Gets the type of the entity (for example, <see cref="ScopeEntityType.Organization"/> or <see cref="ScopeEntityType.Application"/>).
  /// </summary>
  public ScopeEntityType EntityType { get; protected set; }

  /// <summary>
  /// Gets the permission scope granted to the API key for this entity.
  /// </summary>
  public ScopePermission Scope { get; protected set; }

  protected EntityScopeBase(Guid id) : base(id) { }

  /// <summary>
  /// Builds a single ACL entry like "S:&lt;guid&gt;:0001" from this instance.
  /// </summary>
  public string ToAclEntry()
    => $"{ToCode(EntityType)}:{EntityId:D}:{((ushort)Scope):X4}";

  /// <summary>
  /// Maps entity type to compact ACL code (e.g., 'O','A','S','I','K').
  /// </summary>
  private static char ToCode(ScopeEntityType t) => t switch {
    ScopeEntityType.Identity => 'I',
    ScopeEntityType.ApiKey => 'K',
    _ => '?'
  };


}
