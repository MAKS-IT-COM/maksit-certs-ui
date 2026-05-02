using MaksIT.CertsUI.Engine.Abstractions;
using MaksIT.CertsUI.Engine.Facades;
using System.Security.Claims;

namespace MaksIT.CertsUI.Engine.Domain.Identity {
  /// <summary>
  /// Represents a mapping between a <see cref="User"/> and an entity-specific scope.
  /// Defines which permissions a specific user has for a particular entity type and ID.
  /// </summary>
  public class UserEntityScope : EntityScopeBase {
    

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEntityScope"/> class with a new generated scope ID.
    /// </summary>
    public UserEntityScope() : this(CombGui.GenerateCombGuid()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEntityScope"/> class with a specified scope ID.
    /// </summary>
    /// <param name="scopeId">The unique identifier of this scope mapping.</param>
    public UserEntityScope(Guid scopeId) : base(scopeId) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEntityScope"/> class.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="scope">The permission scope assigned to the user.</param>
    public UserEntityScope(Guid entityId, ScopeEntityType entityType, ScopePermission scope)
        : base(CombGui.GenerateCombGuid()) {
      EntityId = entityId;
      EntityType = entityType;
      Scope = scope;
    }

    /// <summary>
    /// Sets the entity identifier for this scope.
    /// </summary>
    /// <param name="entityId">The entity ID to assign.</param>
    /// <returns>The current <see cref="UserEntityScope"/> instance for fluent chaining.</returns>
    public UserEntityScope SetEntityId(Guid entityId) {
      EntityId = entityId;
      return this;
    }

    /// <summary>
    /// Sets the entity type for this scope.
    /// </summary>
    /// <param name="entityType">The entity type to assign.</param>
    /// <returns>The current <see cref="UserEntityScope"/> instance for fluent chaining.</returns>
    public UserEntityScope SetEntityType(ScopeEntityType entityType) {
      EntityType = entityType;
      return this;
    }

    /// <summary>
    /// Sets the permission scope for this entity.
    /// </summary>
    /// <param name="scope">The permission mask to assign.</param>
    /// <returns>The current <see cref="UserEntityScope"/> instance for fluent chaining.</returns>
    public UserEntityScope SetScope(ScopePermission scope) {
      Scope = scope;
      return this;
    }
  }
}
