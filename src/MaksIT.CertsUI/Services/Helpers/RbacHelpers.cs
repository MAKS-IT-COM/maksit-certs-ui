using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine;
using MaksIT.Results;


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

  /// <summary>Only global admins may create users/API keys with <c>IsGlobalAdmin</c> set.</summary>
  public static Result EnsureActorMayAssignGlobalAdmin(JwtTokenData actor, bool requestedIsGlobalAdmin) {
    if (requestedIsGlobalAdmin && !actor.IsGlobalAdmin)
      return Result.Forbidden("Only a global admin can create or assign global admin privileges.");
    return Result.Ok();
  }

  /// <summary>Only global admins may patch the <c>IsGlobalAdmin</c> flag (assign or remove).</summary>
  public static Result EnsureActorMayPatchGlobalAdminFlag(JwtTokenData actor, MaksIT.Core.Abstractions.Webapi.PatchRequestModelBase request, string propertyName) {
    if (request.TryGetOperation(propertyName, out _) && !actor.IsGlobalAdmin)
      return Result.Forbidden("Only a global admin can assign or remove the global admin flag.");
    return Result.Ok();
  }
}

