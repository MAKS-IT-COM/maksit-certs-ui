using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

/// <summary>
/// Placeholder: Certs UI does not store API key entity scopes in PostgreSQL. Returns empty results so the API contract matches Vault without failing callers.
/// Replace with a Linq2Db implementation when scope rows exist.
/// </summary>
public sealed class ApiKeyEntityScopeQueryServiceStub(ILogger<ApiKeyEntityScopeQueryServiceStub> logger) : IApiKeyEntityScopeQueryService {

  public Result<List<ApiKeyEntityScopeQueryResult>?> Search(
    Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit) {
    _ = predicate;
    _ = skip;
    _ = limit;
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Api key entity scope search is not persisted in Certs; returning empty list.");
    return Result<List<ApiKeyEntityScopeQueryResult>?>.Ok([]);
  }

  public Result<int?> Count(Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate) {
    _ = predicate;
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Api key entity scope count is not persisted in Certs; returning 0.");
    return Result<int?>.Ok(0);
  }
}
