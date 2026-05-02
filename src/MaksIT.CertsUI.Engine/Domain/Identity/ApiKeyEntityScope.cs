using MaksIT.CertsUI.Engine.Abstractions;
using MaksIT.CertsUI.Engine.Facades;
using System.Security.Claims;

namespace MaksIT.CertsUI.Engine.Domain.Identity {
  /// <summary>
  /// Represents a mapping between an API key and an entity-specific scope.
  /// Defines which permissions an API key has for a specific entity type and ID.
  /// </summary>
  public class ApiKeyEntityScope : EntityScopeBase {
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEntityScope"/> class with a new generated scope ID.
    /// </summary>
    public ApiKeyEntityScope() : this(CombGui.GenerateCombGuid()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEntityScope"/> class with a specified scope ID.
    /// </summary>
    /// <param name="scopeId">The unique identifier of this scope mapping.</param>
    public ApiKeyEntityScope(Guid scopeId) : base(scopeId) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEntityScope"/> class.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="scope">The permission scope assigned to the API key.</param>
    public ApiKeyEntityScope(Guid entityId, ScopeEntityType entityType, ScopePermission scope)
        : base(CombGui.GenerateCombGuid()) {
      EntityId = entityId;
      EntityType = entityType;
      Scope = scope;
    }

    /// <summary>
    /// Sets the entity identifier for this scope.
    /// </summary>
    /// <param name="entityId">The entity ID to assign.</param>
    /// <returns>The current <see cref="ApiKeyEntityScope"/> instance for fluent chaining.</returns>
    public ApiKeyEntityScope SetEntityId(Guid entityId) {
      EntityId = entityId;
      return this;
    }

    /// <summary>
    /// Sets the entity type for this scope.
    /// </summary>
    /// <param name="entityType">The entity type to assign.</param>
    /// <returns>The current <see cref="ApiKeyEntityScope"/> instance for fluent chaining.</returns>
    public ApiKeyEntityScope SetEntityType(ScopeEntityType entityType) {
      EntityType = entityType;
      return this;
    }

    /// <summary>
    /// Sets the permission scope for this entity.
    /// </summary>
    /// <param name="scope">The permission mask to assign.</param>
    /// <returns>The current <see cref="ApiKeyEntityScope"/> instance for fluent chaining.</returns>
    public ApiKeyEntityScope SetScope(ScopePermission scope) {
      Scope = scope;
      return this;
    }
  }
}
