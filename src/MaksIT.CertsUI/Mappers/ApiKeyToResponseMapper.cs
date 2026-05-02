using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Models.APIKeys;
using MaksIT.CertsUI.Models.APIKeys.Search;

namespace MaksIT.CertsUI.Mappers;

/// <summary>
/// Maps ApiKey / ApiKeyAuthorization and ApiKeyQueryResult to API response models.
/// Used by ApiKeyService.
/// </summary>
public class ApiKeyToResponseMapper {

  public ApiKeyResponse MapToResponse(ApiKey domain, ApiKeyAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(domain);
    return new ApiKeyResponse {
      Id = domain.Id,
      ApiKey = domain.Value,
      IsGlobalAdmin = authorization?.IsGlobalAdmin ?? false,
      EntityScopes = [.. (authorization?.EntityScopes ?? []).Select(sc => new ApiKeyEntityScopeRsponse {
        Id = sc.Id,
        EntityId = sc.EntityId,
        EntityType = sc.EntityType,
        Scope = sc.Scope
      })],
      CreatedAt = domain.CreatedAt,
      Description = domain.Description,
      ExpiresAt = domain.ExpiresAt
    };
  }

  public SearchAPIKeyResponse MapToSearchResponse(ApiKeyQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchAPIKeyResponse {
      Id = queryResult.Id,
      CreatedAt = queryResult.CreatedAt,
      Description = queryResult.Description,
      ExpiresAt = queryResult.ExpiresAt,
      IsGlobalAdmin = queryResult.IsGlobalAdmin
    };
  }

  public SearchApiKeyEntityScopeResponse MapToSearchResponse(ApiKeyEntityScopeQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchApiKeyEntityScopeResponse {
      Id = queryResult.Id,
      ApiKeyId = queryResult.ApiKeyId,
      Description = queryResult.Description,
      EntityId = queryResult.EntityId,
      EntityName = queryResult.EntityName,
      EntityType = queryResult.EntityType,
      Scope = queryResult.Scope
    };
  }
}
