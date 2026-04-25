using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Models.LetsEncryptServer.ApiKeys;
using MaksIT.Models.LetsEncryptServer.ApiKeys.Search;

namespace MaksIT.CertsUI.Mappers;

/// <summary>
/// Maps ApiKey and ApiKeyQueryResult / ApiKeyEntityScopeQueryResult to API response models.
/// Used by ApiKeyService.
/// </summary>
public class ApiKeyToResponseMapper {

  /// <summary>Maps domain aggregate to wire model; <see cref="ApiKeyResponse.ApiKey"/> is populated only when creating.</summary>
  /// <param name="includePlainKey">True only for the create response (Vault always maps <c>domain.Value</c>; Certs supplies the wire from engine once).</param>
  /// <param name="plainKeyWhenCreated">Format <c>{guid:N}|{secret}</c> from engine; omitted on read/patch.</param>
  public ApiKeyResponse MapToResponse(ApiKey apiKey, bool includePlainKey, string? plainKeyWhenCreated = null) {
    ArgumentNullException.ThrowIfNull(apiKey);
    return new ApiKeyResponse {
      Id = apiKey.Id,
      ApiKey = includePlainKey && plainKeyWhenCreated != null ? plainKeyWhenCreated : string.Empty,
      CreatedAt = apiKey.CreatedAt,
      Description = apiKey.Description,
      ExpiresAt = apiKey.ExpiresAt,
      RevokedAt = apiKey.RevokedAtUtc
    };
  }

  public SearchAPIKeyResponse MapToSearchResponse(ApiKeyQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchAPIKeyResponse {
      Id = queryResult.Id,
      CreatedAt = queryResult.CreatedAt,
      Description = queryResult.Description,
      ExpiresAt = queryResult.ExpiresAt,
      RevokedAt = queryResult.RevokedAt
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
