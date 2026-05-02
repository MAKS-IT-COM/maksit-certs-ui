using MaksIT.CertsUI.Engine.Abstractions;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// Authorization aggregate: holds global admin flag and entity scopes for an API key.
/// Decoupled from the ApiKey aggregate so identity and authorization can evolve independently (symmetric with User/UserAuthorization).
/// Domain behavior (GetAclEntries, HasScope) lives on this entity per DDD.
/// <para>
/// <b>Used by:</b>
/// <list type="bullet">
///   <item>IApiKeyDomainService — ReadApiKeyAuthorization / WriteApiKeyAuthorizationAsync</item>
///   <item>ApiKeyAuthorizationPersistenceService (loads/saves from DB)</item>
///   <item>VaultAuthorizationFilter — builds ApiKeyData from authorization after validating API key</item>
///   <item>APIKeyService — RBAC, Create/Patch API key, response mapping</item>
/// </list>
/// </para>
/// </summary>
public class ApiKeyAuthorization {

  /// <summary>
  /// API key this authorization belongs to.
  /// </summary>
  public Guid ApiKeyId { get; private set; }

  /// <summary>
  /// Whether the API key has global admin privileges.
  /// </summary>
  public bool IsGlobalAdmin { get; private set; }

  /// <summary>
  /// Entity-specific scopes (e.g. organization/application permissions).
  /// </summary>
  public List<ApiKeyEntityScope> EntityScopes { get; private set; } = [];

  /// <summary>
  /// Creates authorization for an API key (e.g. when creating a new key).
  /// </summary>
  public ApiKeyAuthorization(Guid apiKeyId) {
    ApiKeyId = apiKeyId;
  }

  /// <summary>
  /// Creates authorization with full data (e.g. when loading from persistence).
  /// </summary>
  public ApiKeyAuthorization(Guid apiKeyId, bool isGlobalAdmin, List<ApiKeyEntityScope> entityScopes) {
    ApiKeyId = apiKeyId;
    IsGlobalAdmin = isGlobalAdmin;
    EntityScopes = entityScopes ?? [];
  }

  public ApiKeyAuthorization SetIsGlobalAdmin(bool isGlobalAdmin) {
    IsGlobalAdmin = isGlobalAdmin;
    return this;
  }

  public ApiKeyAuthorization SetEntityScope(ApiKeyEntityScope entityScope) =>
    SetEntityScopes([entityScope]);

  /// <summary>Sets entity scopes.</summary>
  public ApiKeyAuthorization SetEntityScopes(List<ApiKeyEntityScope>? entityScopes) {
    EntityScopes = entityScopes ?? [];
    return this;
  }

  public ApiKeyAuthorization RemoveEntityScope(Guid entityScopeId) {
    EntityScopes = [.. EntityScopes.Where(x => x.Id != entityScopeId)];
    return this;
  }

  public ApiKeyAuthorization UpsertEntityScope(ApiKeyEntityScope entityScope) {
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
  /// Builds ACL entries for claims (entity scopes + global:admin if IsGlobalAdmin).
  /// </summary>
  public List<string> GetAclEntries() {
    var aclEntries = EntityScopes.Select(sc => sc.ToAclEntry()).ToList();
    if (IsGlobalAdmin)
      aclEntries.Add("global:admin");
    return aclEntries;
  }
}
