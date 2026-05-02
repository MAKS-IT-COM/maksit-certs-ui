using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Services.Helpers;

/// <summary>
/// Small, composable helpers to DRY up RBAC and patch logic around
/// EntityScopes (flags-based) across Identity / API Keys.
/// </summary>
public static class RbacHelpers {

  public static bool Has(ScopePermission p, ScopePermission need) => (p & need) == need;

  public static HashSet<Guid> GetEntityIdsWithScope(this List<IdentityScopeData>? entityScopes, ScopeEntityType entityType, ScopePermission scope) => entityScopes?
    .Where(s => s.EntityType == entityType && Has(s.Scope, scope))
    .Select(s => s.EntityId)
    .ToHashSet() ?? [];
}

