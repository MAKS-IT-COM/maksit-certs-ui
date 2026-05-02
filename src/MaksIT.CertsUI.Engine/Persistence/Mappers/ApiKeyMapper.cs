using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Mappers;

/// <summary>
/// Maps between ApiKey / ApiKeyAuthorization and ApiKeyDto. Used by API key and API key authorization persistence.
/// </summary>
public class ApiKeyMapper {

  public static ApiKey MapToDomain(ApiKeyDto dto) {
    ArgumentNullException.ThrowIfNull(dto);
    return new ApiKey(dto.Id, dto.ApiKey, dto.CreatedAt)
      .SetDescription(dto.Description)
      .SetExpiresAt(dto.ExpiresAt);
  }

  public static ApiKeyDto MapToDto(ApiKey apiKey) {
    ArgumentNullException.ThrowIfNull(apiKey);

    return new ApiKeyDto {
      Id = apiKey.Id,
      ApiKey = apiKey.Value,
      Description = apiKey.Description,
      CreatedAt = apiKey.CreatedAt,
      ExpiresAt = apiKey.ExpiresAt
    };
  }

  /// <summary>Applies authorization to a DTO (IsGlobalAdmin and EntityScopes). Syncs EntityScopes in place so the persistence layer can diff existing vs desired without duplicate-key issues.</summary>
  public static void ApplyAuthorizationToDto(ApiKeyDto dto, Guid apiKeyId, ApiKeyAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(dto);
    dto.IsGlobalAdmin = authorization?.IsGlobalAdmin ?? false;
    SyncEntityScopesToDto(dto.EntityScopes, authorization?.EntityScopes ?? [], apiKeyId);
  }

  /// <summary>Syncs a DTO's EntityScopes list to match the domain list. Domain guarantees unique Ids in newScopes.</summary>
  private static void SyncEntityScopesToDto(List<ApiKeyEntityScopeDto> current, List<ApiKeyEntityScope> newScopes, Guid apiKeyId) {
    var desired = ToEntityScopeDtos(newScopes, apiKeyId);
    var desiredKeys = desired.Select(s => (s.EntityId, s.EntityType, s.Scope)).ToHashSet();

    for (var i = current.Count - 1; i >= 0; i--) {
      var c = current[i];
      if (!desiredKeys.Contains((c.EntityId, c.EntityType, c.Scope)))
        current.RemoveAt(i);
    }

    foreach (var sc in desired) {
      var match = current.FirstOrDefault(x =>
        x.EntityId == sc.EntityId && x.EntityType == sc.EntityType && x.Scope == sc.Scope);

      if (match != null) {
        match.Id = sc.Id;
        match.EntityId = sc.EntityId;
        match.EntityType = sc.EntityType;
        match.Scope = sc.Scope;
      }
      else {
        current.Add(new ApiKeyEntityScopeDto {
          Id = sc.Id,
          ApiKeyId = apiKeyId,
          EntityId = sc.EntityId,
          EntityType = sc.EntityType,
          Scope = sc.Scope
        });
      }
    }
  }

  /// <summary>Builds ApiKeyAuthorization from an ApiKeyDto (read path for authorization persistence).</summary>
  public static ApiKeyAuthorization ToAuthorization(ApiKeyDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    return new ApiKeyAuthorization(
      dto.Id,
      dto.IsGlobalAdmin,
      [.. (dto.EntityScopes ?? []).Select(scope => new ApiKeyEntityScope(scope.Id)
        .SetEntityId(scope.EntityId)
        .SetEntityType(scope.EntityType)
        .SetScope(scope.Scope)
      )]);
  }

  public static List<ApiKeyEntityScopeDto> ToEntityScopeDtos(IEnumerable<ApiKeyEntityScope> scopes, Guid apiKeyId) {
    return [.. scopes.Select(scope => new ApiKeyEntityScopeDto {
      Id = scope.Id,
      ApiKeyId = apiKeyId,
      EntityId = scope.EntityId,
      EntityType = scope.EntityType,
      Scope = scope.Scope
    })];
  }
}
