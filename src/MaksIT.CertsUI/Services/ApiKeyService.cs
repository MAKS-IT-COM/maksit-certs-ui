using MaksIT.Core.Extensions;
using MaksIT.Core.Webapi.Models;
using MaksIT.Results;
using MaksIT.CertsUI.Trng;
using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Models.APIKeys;
using MaksIT.CertsUI.Models.APIKeys.Search;
using MaksIT.CertsUI.Models.Identity.User;
using MaksIT.CertsUI.Services.Helpers;
using MaksIT.CertsUI.Mappers;
using Microsoft.Extensions.Options;
using System.Data;
using System.Linq.Expressions;


namespace MaksIT.CertsUI.Services;

public interface IApiKeyService {
  #region Search
  Result<PagedResponse<SearchAPIKeyResponse>?> SearchApiKeys(JwtTokenData jwtTokenData, SearchAPIKeyRequest requestData);
  Result<PagedResponse<SearchApiKeyEntityScopeResponse>?> SearchApiKeyEntityScopes(JwtTokenData jwtTokenData, SearchApiKeyEntityScopeRequest requestData);
  #endregion

  #region Read
  Result<ApiKeyResponse?> ReadAPIKey(JwtTokenData jwtTokenData, Guid apiKeyId);
  #endregion

  #region Create
  Task<Result<ApiKeyResponse?>> CreateAPIKeyAsync(JwtTokenData jwtTokenData, CreateApiKeyRequest requestData);
  #endregion

  #region Patch
  Task<Result<ApiKeyResponse?>> PatchAPIKeyAsync(JwtTokenData jwtTokenData, Guid id, PatchApiKeyRequest requestData);
  #endregion

  #region Delete
  Task<Result> DeleteAPIKeyAsync(JwtTokenData jwtTokenData, Guid apiKeyId);
  #endregion
}

public class ApiKeyService(
    ILogger<ApiKeyService> logger,
    IOptions<Configuration> appSettings,
    IIdentityDomainService identityDomainService,
    IApiKeyQueryService apiKeyQueryService,
    IApiKeyEntityScopeQueryService apiKeyEntityScopeQueryService,
    IApiKeyDomainService apiKeyDomainService,
    ITrngClient trngClient,
    ApiKeyToResponseMapper apiKeyToResponseMapper
) : ServiceBase<ApiKeyResponse, ApiKey, SearchAPIKeyResponse, ApiKeyQueryResult>(logger, appSettings), IApiKeyService {
  private readonly IIdentityDomainService _identityDomainService = identityDomainService;
  private readonly IApiKeyQueryService _apiKeyQueryService = apiKeyQueryService;
  private readonly IApiKeyEntityScopeQueryService _apiKeyEntityScopeQueryService = apiKeyEntityScopeQueryService;
  private readonly IApiKeyDomainService _apiKeyDomainService = apiKeyDomainService;
  private readonly ITrngClient _trngClient = trngClient;
  private readonly ApiKeyToResponseMapper _apiKeyToResponseMapper = apiKeyToResponseMapper;

  #region API Keys RBAC

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to read the specified API key.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can read any API key.</item>
  ///   <item>Non-admin users must have <c>Read</c> permission on <c>ApiKey</c> scope for every organization the API key belongs to.</item>
  ///   <item>If none of the above conditions are met, access is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="apiKeyId">The ID of the API key to be read.</param>
  /// <returns>
  /// A <see cref="Result{ApiKey}"/> indicating success (with the target API key) or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result<ApiKey?> ReadApiKeyRBAC(JwtTokenData jwtTokenData, Guid apiKeyId) {
    var apiKeyResult = _apiKeyDomainService.ReadAPIKey(apiKeyId);

    if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null)
      return apiKeyResult.ToResultOfType<ApiKey?>(_ => null);

    var apiKey = apiKeyResult.Value;
    var authResult = _apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;

    return RBACWrapperJwtToken(
        jwtTokenData,
        apiKey,
        userRules: (_) => {
          

         
            return Result<ApiKey?>.Ok(apiKey);
         
        }
    );
  }

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to create an API key.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can create any API key.</item>
  ///   <item>Only Global Admin can set <c>IsGlobalAdmin</c> on the new API key.</item>
  ///   <item>Non-admin users must have <c>Create</c> permission on <c>ApiKey</c> scope for every organization in the create request.</item>
  ///   <item>If none of the above conditions are met, creation is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="requestData">The create API key request data.</param>
  /// <returns>
  /// A <see cref="Result"/> indicating success or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result CreateApiKeyRBAC(JwtTokenData jwtTokenData, CreateApiKeyRequest requestData) => RBACWrapperJwtToken(
    jwtTokenData,
    (jwtTokenData) => {
        return Result.Ok();
    });

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to patch (update) the specified API key.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can patch any API key, including the <c>IsGlobalAdmin</c> flag.</item>
  ///   <item>Only Global Admin can assign or remove the <c>IsGlobalAdmin</c> flag.</item>
  ///   <item>Non-admin users must have <c>Write</c> permission on <c>ApiKey</c> scope for all organizations the API key belongs to.</item>
  ///   <item>If the patch modifies organization scopes, the user must have <c>Write</c> on <c>ApiKey</c> scope for all affected organizations.</item>
  ///   <item>If none of the above conditions are met, patching is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="apiKeyId">The ID of the API key to be patched.</param>
  /// <param name="requestData">The patch request data.</param>
  /// <returns>
  /// A <see cref="Result{ApiKey}"/> indicating success (with the target API key) or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result<ApiKey?> PatchApiKeyRBAC(JwtTokenData jwtTokenData, Guid apiKeyId, PatchApiKeyRequest requestData) {

    var apiKeyResult = _apiKeyDomainService.ReadAPIKey(apiKeyId);

    if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null)
      return apiKeyResult.ToResultOfType<ApiKey?>(_ => null);

    var apiKey = apiKeyResult.Value;
    var authResult = _apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;

    return RBACWrapperJwtToken(
      jwtTokenData,
      apiKey,
      (_) => {
        return Result<ApiKey?>.Ok(apiKey);
      });
  }

  /// <summary>
  /// Performs RBAC (Role-Based Access Control) checks to determine if the current user is authorized to delete the specified API key.
  /// <para>
  /// Enforces the following rules:
  /// <list type="bullet">
  ///   <item>Global Admin can delete any API key.</item>
  ///   <item>Non-admin users must have <c>Delete</c> permission on <c>ApiKey</c> scope for every organization the API key belongs to.</item>
  ///   <item>If none of the above conditions are met, deletion is forbidden.</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="jwtTokenData">The JWT token data of the acting user.</param>
  /// <param name="apiKeyId">The ID of the API key to be deleted.</param>
  /// <returns>
  /// A <see cref="Result"/> indicating success or a forbidden result with an appropriate message.
  /// </returns>
  /// <remarks>
  /// Last update: 02/03/2026
  /// </remarks>
  private Result DeleteApiKeyRBAC(JwtTokenData jwtTokenData, Guid apiKeyId) {
    var apiKeyResult = _apiKeyDomainService.ReadAPIKey(apiKeyId);
    if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null)
      return apiKeyResult;

    var apiKey = apiKeyResult.Value;
    var authResult = _apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;

    return RBACWrapperJwtToken(
      jwtTokenData,
      apiKey,
      (_) => {
          return apiKeyResult;
      }
    ).ToResult();
  }
  #endregion

  #region Search

  /// <summary>
  /// Returns a paged list of API keys the acting user is authorized to view.
  /// - Global Admin can view all API keys.
  /// - Non-admins can only view API keys for which they have Read (Organization) or Read (ApiKey) for all of the key's orgs.
  /// </summary>
  public Result<PagedResponse<SearchAPIKeyResponse>?> SearchApiKeys(JwtTokenData jwtTokenData, SearchAPIKeyRequest requestData) {
    var requestFilter = (requestData.BuildFilterExpression<ApiKeyDto>() ?? (k => true));

    if (jwtTokenData.IsGlobalAdmin)
      return ExecuteSearch(requestData, requestFilter);

    var apiKeyReadIds = (jwtTokenData.EntityScopes ?? Enumerable.Empty<IdentityScopeData>())
        .Where(sc => sc.EntityType == ScopeEntityType.ApiKey && RbacHelpers.Has(sc.Scope, ScopePermission.Read))
        .Select(sc => sc.EntityId)
        .ToHashSet();
    var visibleOrgIds = apiKeyReadIds;

    Expression<Func<ApiKeyDto, bool>> accessScope = k =>
      !k.EntityScopes
        .Select(es => es.EntityId)
        .Except(visibleOrgIds)
        .Any();

    var finalPredicate = accessScope.AndAlso(requestFilter);
    return ExecuteSearch(requestData, finalPredicate);
  }

  private Result<PagedResponse<SearchAPIKeyResponse>?> ExecuteSearch(SearchAPIKeyRequest requestData, Expression<Func<ApiKeyDto, bool>>? apiKeyPredicate) {
    var skip = (requestData.PageNumber - 1) * requestData.PageSize;
    var take = requestData.PageSize;

    var apiKeyResult = _apiKeyQueryService.Search(apiKeyPredicate, skip, take);

    if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null)
      return apiKeyResult.ToResultOfType<PagedResponse<SearchAPIKeyResponse>?>(_ => null);

    var apiKeys = apiKeyResult.Value;

    var apiKeysCountResult = _apiKeyQueryService.Count(apiKeyPredicate);

    if (!apiKeysCountResult.IsSuccess || apiKeysCountResult.Value == null)
      return apiKeysCountResult.ToResultOfType<PagedResponse<SearchAPIKeyResponse>?>(_ => null);

    var apiKeysCount = apiKeysCountResult.Value ?? 0;

    var pagedResponse = new PagedResponse<SearchAPIKeyResponse>(
        apiKeys.Select(MapToSearchResponse),
        apiKeysCount,
        requestData.PageNumber,
        requestData.PageSize
    );

    return Result<PagedResponse<SearchAPIKeyResponse>?>.Ok(pagedResponse);
  }

  /// <summary>
  /// Returns a paged list of API key entity scopes the acting user is authorized to view.
  /// Global Admin sees all; others see only scopes for API keys they can read (same visible-org logic as SearchApiKeys).
  /// </summary>
  public Result<PagedResponse<SearchApiKeyEntityScopeResponse>?> SearchApiKeyEntityScopes(JwtTokenData jwtTokenData, SearchApiKeyEntityScopeRequest requestData) {
    Expression<Func<ApiKeyEntityScopeDto, bool>>? requestFilter = requestData.BuildFilterExpression();
    if (requestData.ApiKeyId.HasValue)
      requestFilter = (requestFilter ?? (s => true)).AndAlso(s => s.ApiKeyId == requestData.ApiKeyId.Value);

    if (jwtTokenData.IsGlobalAdmin)
      return ExecuteSearchApiKeyEntityScopes(requestData, requestFilter);

    var apiKeyReadIds = (jwtTokenData.EntityScopes ?? Enumerable.Empty<IdentityScopeData>())
        .Where(sc => sc.EntityType == ScopeEntityType.ApiKey && RbacHelpers.Has(sc.Scope, ScopePermission.Read))
        .Select(sc => sc.EntityId)
        .ToHashSet();
    var visibleOrgIds = apiKeyReadIds;

    Expression<Func<ApiKeyDto, bool>> accessScope = k =>
      !k.EntityScopes
        .Select(es => es.EntityId)
        .Except(visibleOrgIds)
        .Any();

    var apiKeysResult = _apiKeyQueryService.Search(accessScope, 0, 50000);
    var allowedApiKeyIds = new HashSet<Guid>();
    if (apiKeysResult.IsSuccess && apiKeysResult.Value != null && apiKeysResult.Value.Count > 0)
      allowedApiKeyIds = apiKeysResult.Value.Select(k => k.Id).ToHashSet();

    Expression<Func<ApiKeyEntityScopeDto, bool>> scopeAccessFilter = s => allowedApiKeyIds.Contains(s.ApiKeyId);
    var finalPredicate = scopeAccessFilter.AndAlso(requestFilter ?? (s => true));
    return ExecuteSearchApiKeyEntityScopes(requestData, finalPredicate);
  }

  private Result<PagedResponse<SearchApiKeyEntityScopeResponse>?> ExecuteSearchApiKeyEntityScopes(SearchApiKeyEntityScopeRequest requestData, Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate) {
    var skip = (requestData.PageNumber - 1) * requestData.PageSize;
    var take = requestData.PageSize;

    var scopesResult = _apiKeyEntityScopeQueryService.Search(predicate, skip, take);
    if (!scopesResult.IsSuccess || scopesResult.Value == null)
      return scopesResult.ToResultOfType<PagedResponse<SearchApiKeyEntityScopeResponse>?>(_ => null);

    var scopes = scopesResult.Value;
    var countResult = _apiKeyEntityScopeQueryService.Count(predicate);
    if (!countResult.IsSuccess || countResult.Value == null)
      return countResult.ToResultOfType<PagedResponse<SearchApiKeyEntityScopeResponse>?>(_ => null);

    var totalCount = countResult.Value ?? 0;
    var pagedResponse = new PagedResponse<SearchApiKeyEntityScopeResponse>(
        scopes.Select(_apiKeyToResponseMapper.MapToSearchResponse),
        totalCount,
        requestData.PageNumber,
        requestData.PageSize
    );
    return Result<PagedResponse<SearchApiKeyEntityScopeResponse>?>.Ok(pagedResponse);
  }

  #endregion

  #region Read

  public Result<ApiKeyResponse?> ReadAPIKey(JwtTokenData jwtTokenData, Guid apiKeyId) {
    var apiKeyResult = ReadApiKeyRBAC(jwtTokenData, apiKeyId);
    if (!apiKeyResult.IsSuccess || apiKeyResult.Value == null)
      return apiKeyResult.ToResultOfType<ApiKeyResponse?>(_ => null);

    var apiKey = apiKeyResult.Value;
    var authResult = _apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
    var authorization = authResult.IsSuccess ? authResult.Value : null;
    var response = MapToResponse(apiKey, authorization);

    return Result<ApiKeyResponse?>.Ok(response);
  }

  #endregion

  #region Create

  public async Task<Result<ApiKeyResponse?>> CreateAPIKeyAsync(JwtTokenData jwtTokenData, CreateApiKeyRequest requestData) {
    var rbacResult = CreateApiKeyRBAC(jwtTokenData, requestData);
    if (!rbacResult.IsSuccess)
      return rbacResult.ToResultOfType<ApiKeyResponse?>(null);

    var trngResult = await _trngClient.GetRandomBytesBase64Async(32);
    if (!trngResult.IsSuccess || trngResult.Value == null)
      return trngResult.ToResultOfType<ApiKeyResponse?>(_ => null);

    var apiKeyValue = trngResult.Value;

    var newApiKey = new ApiKey(apiKeyValue)
      .SetDescription(requestData.Description)
      .SetExpiresAt(requestData.ExpiresAt);

    var authorization = new ApiKeyAuthorization(newApiKey.Id)
      .SetIsGlobalAdmin(requestData.IsGlobalAdmin);
    if (requestData.EntityScopes != null) {
      var groupedByEntityAndType = requestData.EntityScopes
        .GroupBy(role => (role.EntityId, role.EntityType))
        .ToList();
      var entityScopes = groupedByEntityAndType
        .Select(g => new ApiKeyEntityScope(g.First().EntityId, g.First().EntityType, g.First().Scope))
        .ToList();
      authorization.SetEntityScopes(entityScopes);
    }

    var writeKeyResult = await _apiKeyDomainService.WriteAPIKeyAsync(newApiKey, authorization);
    if (!writeKeyResult.IsSuccess || writeKeyResult.Value == null)
      return writeKeyResult.ToResultOfType<ApiKeyResponse?>(_ => null);

    var apiKey = writeKeyResult.Value;

    var response = MapToResponse(apiKey, authorization);

    return Result<ApiKeyResponse?>.Ok(response);
  }

  #endregion

  #region Patch

  public async Task<Result<ApiKeyResponse?>> PatchAPIKeyAsync(JwtTokenData jwtTokenData, Guid id, PatchApiKeyRequest requestData) {
    var rbac = PatchApiKeyRBAC(jwtTokenData, id, requestData);
    if (!rbac.IsSuccess || rbac.Value == null)
      return rbac.ToResultOfType<ApiKeyResponse?>(_ => null);

    var apiKey = rbac.Value;
    var authResult = _apiKeyDomainService.ReadApiKeyAuthorization(apiKey.Id);
    var authorization = authResult.IsSuccess && authResult.Value != null ? authResult.Value : new ApiKeyAuthorization(apiKey.Id);

    // 1) Patch API key master data (description, expiry)
    var masterDataResult = PatchApiKeyMasterData(apiKey, requestData);
    if (!masterDataResult.IsSuccess)
      return masterDataResult;

    // 2) Patch authorization fields (IsGlobalAdmin, EntityScopes)
    var authPatchResult = PatchApiKeyAuthorization(authorization, requestData);
    if (!authPatchResult.IsSuccess)
      return authPatchResult;

    var upsertKeyResult = await _apiKeyDomainService.WriteAPIKeyAsync(apiKey, authorization);
    if (!upsertKeyResult.IsSuccess || upsertKeyResult.Value == null)
      return upsertKeyResult.ToResultOfType<ApiKeyResponse?>(_ => null);

    apiKey = upsertKeyResult.Value;

    var apiKeyResponse = MapToResponse(apiKey, authorization);
    return Result<ApiKeyResponse?>.Ok(apiKeyResponse);
  }

  #endregion

  /// <summary>
  /// Applies master-data patches (description, ExpiresAt) to the given API key.
  /// </summary>
  private Result<ApiKeyResponse?> PatchApiKeyMasterData(ApiKey apiKey, PatchApiKeyRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.Description), out var operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (string.IsNullOrWhiteSpace(requestData.Description))
            return PatchFieldIsNotDefined<ApiKeyResponse?>(nameof(requestData.Description));
          apiKey.SetDescription(requestData.Description);
          break;

        case PatchOperation.RemoveField:
          apiKey.SetDescription(null);
          break;

        default:
          return UnsupportedPatchOperationResponse<ApiKeyResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.ExpiresAt), out operation)) {
      switch (operation) {
        case PatchOperation.SetField:
          if (requestData.ExpiresAt == null)
            return PatchFieldIsNotDefined<ApiKeyResponse?>(nameof(requestData.ExpiresAt));
          apiKey.SetExpiresAt(requestData.ExpiresAt);
          break;

        case PatchOperation.RemoveField:
          apiKey.SetExpiresAt(null);
          break;

        default:
          return UnsupportedPatchOperationResponse<ApiKeyResponse?>();
      }
    }

    return Result<ApiKeyResponse?>.Ok(null);
  }

  /// <summary>
  /// Applies authorization patches (IsGlobalAdmin and EntityScopes) to the given API key authorization object.
  /// </summary>
  private Result<ApiKeyResponse?> PatchApiKeyAuthorization(ApiKeyAuthorization authorization, PatchApiKeyRequest requestData) {
    if (requestData.TryGetOperation(nameof(requestData.IsGlobalAdmin), out PatchOperation? _)) {
      if (requestData.IsGlobalAdmin != null)
        authorization.SetIsGlobalAdmin(requestData.IsGlobalAdmin.Value);
    }

    if (requestData.EntityScopes != null) {
      var currentScopes = authorization.EntityScopes.ToList();

      foreach (var scopePatch in requestData.EntityScopes) {
        if (scopePatch.TryGetOperation(Constants.CollectionItemOperation, out var collectionOp)) {
          switch (collectionOp) {
            case PatchOperation.AddToCollection:
              if (!currentScopes.Any(s =>
                  s.EntityId == scopePatch.EntityId &&
                  s.EntityType == scopePatch.EntityType &&
                  s.Scope == scopePatch.Scope)) {
                currentScopes.Add(new ApiKeyEntityScope(scopePatch.EntityId, scopePatch.EntityType, scopePatch.Scope));
              }
              break;

            case PatchOperation.RemoveFromCollection:
              if (scopePatch.Id != null) {
                currentScopes.RemoveAll(s => s.Id == scopePatch.Id);
              }
              else {
                currentScopes.RemoveAll(s =>
                    s.EntityId == scopePatch.EntityId &&
                    s.EntityType == scopePatch.EntityType &&
                    s.Scope == scopePatch.Scope);
              }
              break;

            default:
              return UnsupportedPatchOperationResponse<ApiKeyResponse?>();
          }
        }
        else if (scopePatch.Id != null && scopePatch.Id != Guid.Empty) {
          // Update in place: client sent same scope with changed fields (e.g. permission change) without Add/Remove.
          var existing = currentScopes.FirstOrDefault(s => s.Id == scopePatch.Id);
          if (existing != null)
            existing.SetEntityId(scopePatch.EntityId).SetEntityType(scopePatch.EntityType).SetScope(scopePatch.Scope);
        }
      }

      authorization.SetEntityScopes(currentScopes);
    }

    return Result<ApiKeyResponse?>.Ok(null);
  }

  #region Delete

  public async Task<Result> DeleteAPIKeyAsync(JwtTokenData jwtTokenData, Guid id) {
    var rbacResult = DeleteApiKeyRBAC(jwtTokenData, id);
    if (!rbacResult.IsSuccess)
      return rbacResult;

    var deleteResult = await _apiKeyDomainService.DeleteAPIKeyAsync(id);
    return deleteResult;
  }

  #endregion

  #region Map to Response

  protected ApiKeyResponse MapToResponse(ApiKey domain, ApiKeyAuthorization? authorization) =>
    _apiKeyToResponseMapper.MapToResponse(domain, authorization);

  protected override ApiKeyResponse MapToResponse(ApiKey domain) =>
    _apiKeyToResponseMapper.MapToResponse(domain, null);

  #endregion

  #region Map QueryResult to SerchResponse

  protected override SearchAPIKeyResponse MapToSearchResponse(ApiKeyQueryResult queryResult) =>
    _apiKeyToResponseMapper.MapToSearchResponse(queryResult);

  #endregion
}