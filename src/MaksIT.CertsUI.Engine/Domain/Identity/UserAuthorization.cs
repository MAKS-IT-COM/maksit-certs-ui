using MaksIT.CertsUI.Engine.Abstractions;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// Authorization aggregate: holds global admin flag and entity scopes for a user.
/// Decoupled from the User aggregate so identity and authorization can evolve independently.
/// Domain behavior (GetAclEntries, HasScope) lives on this entity per DDD.
/// <para>
/// <b>Used by:</b>
/// <list type="bullet">
///   <item>IIdentityDomainService — ReadUserAuthorization / WriteUserAuthorizationAsync; Login/Refresh call GetAclEntries() for JWT</item>
///   <item>UserAuthorizationPersistenceService (loads/saves from DB)</item>
///   <item>JwtAuthorizationFilter — builds JwtTokenData from authorization after validating token</item>
///   <item>IdentityService — RBAC (Read/Patch/Delete user), Create/Patch user, UserResponse mapping</item>
/// </list>
/// </para>
/// </summary>
public class UserAuthorization(
  Guid userId,
  bool isGlobalAdmin = false,
  List<UserEntityScope>? entityScopes = null
) {

  /// <summary>
  /// User this authorization belongs to.
  /// </summary>
  public Guid UserId { get; private set; } = userId;

  /// <summary>
  /// Whether the user has global admin privileges.
  /// </summary>
  public bool IsGlobalAdmin { get; private set; } = isGlobalAdmin;

  /// <summary>
  /// Entity-specific scopes (e.g. organization/application permissions).
  /// </summary>
  public List<UserEntityScope> EntityScopes { get; private set; } = entityScopes ?? [];

  public UserAuthorization SetIsGlobalAdmin(bool isGlobalAdmin) {
    IsGlobalAdmin = isGlobalAdmin;
    return this;
  }

  public UserAuthorization SetEntityScope(UserEntityScope entityScope) =>
    SetEntityScopes([entityScope]);

  /// <summary>Sets entity scopes.</summary>
  public UserAuthorization SetEntityScopes(List<UserEntityScope>? entityScopes) {
    EntityScopes = entityScopes ?? [];
    return this;
  }

  public UserAuthorization RemoveEntityScope(Guid entityScopeId) {
    EntityScopes = [.. EntityScopes.Where(x => x.Id != entityScopeId)];
    return this;
  }

  public UserAuthorization UpsertEntityScope(UserEntityScope entityScope) {
    var existing = EntityScopes.FirstOrDefault(x => x.Id == entityScope.Id);
    if (existing != null) {
      existing
        .SetEntityId(entityScope.EntityId)
        .SetEntityType(entityScope.EntityType)
        .SetScope(entityScope.Scope);
    }
    else {
      EntityScopes.Add(entityScope);
    }
    return this;
  }

  /// <summary>
  /// Returns whether this authorization grants the given scope for the given entity.
  /// </summary>
  public bool HasScope(Guid entityId, ScopeEntityType entityType, ScopePermission scope) =>
    EntityScopes.Any(x =>
      x.EntityId == entityId &&
      x.EntityType == entityType &&
      x.Scope == scope);

  /// <summary>
  /// Builds ACL entries for JWT claims (entity scopes + global:admin if IsGlobalAdmin).
  /// </summary>
  public List<string> GetAclEntries() {
    var aclEntries = EntityScopes.Select(sc => sc.ToAclEntry()).ToList();
    if (IsGlobalAdmin)
      aclEntries.Add("global:admin");
    return aclEntries;
  }
}
