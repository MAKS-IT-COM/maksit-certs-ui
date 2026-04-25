using MaksIT.CertsUI.Engine.Query;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

/// <summary>
/// Placeholder: Certs UI does not store API key entity scopes in PostgreSQL. Returns an empty page so the API contract matches Vault without failing callers.
/// Replace with a Linq2Db implementation when scope rows exist.
/// </summary>
public sealed class ApiKeyEntityScopeQueryServiceStub(ILogger<ApiKeyEntityScopeQueryServiceStub> logger) : IApiKeyEntityScopeQueryService {

  public Task<Result<PagedQueryResult<ApiKeyEntityScopeQueryResult>>> SearchApiKeyEntityScopesAsync(
    Guid? apiKeyId,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default) {
    _ = apiKeyId;
    cancellationToken.ThrowIfCancellationRequested();
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Api key entity scope search is not persisted in Certs; returning empty page.");
    var page = Math.Max(1, pageNumber);
    var size = Math.Clamp(pageSize, 1, 500);
    return Task.FromResult(Result<PagedQueryResult<ApiKeyEntityScopeQueryResult>>.Ok(
      new PagedQueryResult<ApiKeyEntityScopeQueryResult>([], 0, page, size)));
  }
}
