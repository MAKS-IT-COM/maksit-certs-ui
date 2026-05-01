using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Models.LetsEncryptServer.ApiKeys;
using MaksIT.Models.LetsEncryptServer.ApiKeys.Search;
using MaksIT.Models.LetsEncryptServer.Common;
using MaksIT.Results;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;

namespace MaksIT.CertsUI.Services;

/// <summary>Thin orchestration service: ACL + request translation; business rules live in Engine domain services.</summary>
public interface IApiKeyService {

  #region Validation

  Task<Result<Guid>> TryValidateKeyAsync(string? rawKey, CancellationToken cancellationToken = default);

  #endregion

  #region Search

  Task<Result<PagedResponse<SearchAPIKeyResponse>?>> SearchApiKeysAsync(JwtTokenData jwtTokenData, SearchAPIKeyRequest requestData, CancellationToken cancellationToken = default);

  Task<Result<PagedResponse<SearchApiKeyEntityScopeResponse>?>> SearchApiKeyEntityScopesAsync(JwtTokenData jwtTokenData, SearchApiKeyEntityScopeRequest requestData, CancellationToken cancellationToken = default);

  #endregion

  #region Read

  Task<Result<ApiKeyResponse?>> ReadAPIKeyAsync(JwtTokenData jwtTokenData, Guid apiKeyId, CancellationToken cancellationToken = default);

  #endregion

  #region Create

  Task<Result<ApiKeyResponse?>> CreateAPIKeyAsync(JwtTokenData jwtTokenData, CreateApiKeyRequest requestData, CancellationToken cancellationToken = default);

  #endregion

  #region Patch

  Task<Result<ApiKeyResponse?>> PatchAPIKeyAsync(JwtTokenData jwtTokenData, Guid id, PatchApiKeyRequest requestData, CancellationToken cancellationToken = default);

  #endregion

  #region Delete

  Task<Result> DeleteAPIKeyAsync(JwtTokenData jwtTokenData, Guid id, CancellationToken cancellationToken = default);

  #endregion
}

public sealed class ApiKeyService(
  ILogger<ApiKeyService> logger,
  IApiKeyDomainService apiKeyDomainService,
  IApiKeyQueryService apiKeyQueryService,
  IApiKeyEntityScopeQueryService apiKeyEntityScopeQueryService,
  ApiKeyToResponseMapper apiKeyToResponseMapper
) : IApiKeyService {

  #region Validation

  public Task<Result<Guid>> TryValidateKeyAsync(string? rawKey, CancellationToken cancellationToken = default) =>
    apiKeyDomainService.TryValidateKeyAsync(rawKey, cancellationToken);

  #endregion

  #region Search

  public async Task<Result<PagedResponse<SearchAPIKeyResponse>?>> SearchApiKeysAsync(
    JwtTokenData _jwtTokenData,
    SearchAPIKeyRequest requestData,
    CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;
    var page = Math.Max(1, requestData.PageNumber);
    var size = Math.Clamp(requestData.PageSize, 1, 500);
    var filter = requestData.DescriptionFilter?.Trim();
    Expression<Func<ApiKeyDto, bool>>? predicate = string.IsNullOrWhiteSpace(filter)
      ? null
      : k => (k.Description ?? string.Empty).Contains(filter!);

    var skip = (page - 1) * size;
    var countResult = apiKeyQueryService.Count(predicate);
    if (!countResult.IsSuccess)
      return countResult.ToResultOfType<PagedResponse<SearchAPIKeyResponse>?>(_ => null);

    var searchResult = apiKeyQueryService.Search(predicate, skip, size);
    if (!searchResult.IsSuccess || searchResult.Value == null)
      return searchResult.ToResultOfType<PagedResponse<SearchAPIKeyResponse>?>(_ => null);

    var total = countResult.Value ?? 0;
    var list = searchResult.Value;
    return Result<PagedResponse<SearchAPIKeyResponse>?>.Ok(new PagedResponse<SearchAPIKeyResponse> {
      Data = [.. list.Select(apiKeyToResponseMapper.MapToSearchResponse)],
      TotalRecords = total,
      PageNumber = page,
      PageSize = size,
    });
  }

  /// <summary>Vault exposes <c>POST scopes/search</c>; Certs uses async query + stub until scope rows exist.</summary>
  public async Task<Result<PagedResponse<SearchApiKeyEntityScopeResponse>?>> SearchApiKeyEntityScopesAsync(
    JwtTokenData _jwtTokenData,
    SearchApiKeyEntityScopeRequest requestData,
    CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;
    var page = Math.Max(1, requestData.PageNumber);
    var size = Math.Clamp(requestData.PageSize, 1, 500);
    Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate = requestData.ApiKeyId.HasValue
      ? s => s.ApiKeyId == requestData.ApiKeyId!.Value
      : null;

    var skip = (page - 1) * size;
    var countResult = apiKeyEntityScopeQueryService.Count(predicate);
    if (!countResult.IsSuccess)
      return countResult.ToResultOfType<PagedResponse<SearchApiKeyEntityScopeResponse>?>(_ => null);

    var searchResult = apiKeyEntityScopeQueryService.Search(predicate, skip, size);
    if (!searchResult.IsSuccess || searchResult.Value == null)
      return searchResult.ToResultOfType<PagedResponse<SearchApiKeyEntityScopeResponse>?>(_ => null);

    var total = countResult.Value ?? 0;
    var list = searchResult.Value;
    return Result<PagedResponse<SearchApiKeyEntityScopeResponse>?>.Ok(new PagedResponse<SearchApiKeyEntityScopeResponse> {
      Data = [.. list.Select(apiKeyToResponseMapper.MapToSearchResponse)],
      TotalRecords = total,
      PageNumber = page,
      PageSize = size,
    });
  }

  #endregion

  #region Read

  public async Task<Result<ApiKeyResponse?>> ReadAPIKeyAsync(JwtTokenData _jwtTokenData, Guid apiKeyId, CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;
    var row = await apiKeyDomainService.ReadAPIKeyAsync(apiKeyId, cancellationToken);
    if (!row.IsSuccess || row.Value == null)
      return row.ToResultOfType<ApiKeyResponse?>(_ => null);

    return Result<ApiKeyResponse?>.Ok(apiKeyToResponseMapper.MapToResponse(row.Value, includePlainKey: false));
  }

  #endregion

  #region Create

  /// <summary>
  /// Orchestrates API key creation (Vault: <c>MaksIT.Vault.Services.ApiKeyService.CreateAPIKeyAsync</c> — TRNG + domain write + map).
  /// Certs engine returns hash-backed storage and a one-time plaintext wire; this method maps that to <see cref="ApiKeyResponse"/>.
  /// </summary>
  public async Task<Result<ApiKeyResponse?>> CreateAPIKeyAsync(
    JwtTokenData _jwtTokenData,
    CreateApiKeyRequest requestData,
    CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;

    var created = await apiKeyDomainService.CreateAPIKeyAsync(requestData.Description, requestData.ExpiresAt, cancellationToken);
    if (!created.IsSuccess)
      return created.ToResultOfType<ApiKeyResponse?>(_ => null);

    var bundle = created.Value;
    if (!bundle.HasValue) {
      logger.LogError("CreateAPIKeyAsync: domain reported success but returned an empty payload.");
      return Result<ApiKeyResponse?>.InternalServerError(null, ["API key payload was missing after create."]);
    }

    var apiKey = bundle.Value.ApiKey;
    var plainWire = bundle.Value.PlainKey;

    logger.LogInformation("Created API key {ApiKeyId}.", apiKey.Id);
    return Result<ApiKeyResponse?>.Ok(apiKeyToResponseMapper.MapToResponse(apiKey, includePlainKey: true, plainKeyWhenCreated: plainWire));
  }

  #endregion

  #region Patch

  public async Task<Result<ApiKeyResponse?>> PatchAPIKeyAsync(
    JwtTokenData _jwtTokenData,
    Guid id,
    PatchApiKeyRequest requestData,
    CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;

    string? description = null;
    DateTime? expiresAtUtc = null;
    bool removeExpiry = false;

    if (requestData.TryGetOperation(nameof(requestData.Description), out var descriptionOp)) {
      switch (descriptionOp) {
        case MaksIT.Core.Webapi.Models.PatchOperation.SetField:
          description = requestData.Description;
          break;
        default:
          return Result<ApiKeyResponse?>.BadRequest(null, "Unsupported patch operation for description.");
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.ExpiresAt), out var expiresOp)) {
      switch (expiresOp) {
        case MaksIT.Core.Webapi.Models.PatchOperation.SetField:
          expiresAtUtc = requestData.ExpiresAt;
          break;
        case MaksIT.Core.Webapi.Models.PatchOperation.RemoveField:
          removeExpiry = true;
          break;
        default:
          return Result<ApiKeyResponse?>.BadRequest(null, "Unsupported patch operation for expiresAt.");
      }
    }

    var write = await apiKeyDomainService.WriteAPIKeyAsync(id, description, expiresAtUtc, removeExpiry, cancellationToken);
    if (!write.IsSuccess || write.Value == null)
      return write.ToResultOfType<ApiKeyResponse?>(_ => null);

    return Result<ApiKeyResponse?>.Ok(apiKeyToResponseMapper.MapToResponse(write.Value, includePlainKey: false));
  }

  #endregion

  #region Delete

  public async Task<Result> DeleteAPIKeyAsync(JwtTokenData _jwtTokenData, Guid id, CancellationToken cancellationToken = default) {
    _ = _jwtTokenData;
    var deleted = await apiKeyDomainService.DeleteAPIKeyAsync(id, cancellationToken);
    if (deleted.IsSuccess)
      logger.LogInformation("Deleted API key {ApiKeyId}.", id);
    return deleted;
  }

  #endregion
}
