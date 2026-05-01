using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// API key ↔ entity scope search (Vault <c>IApiKeyEntityScopeQueryService</c> pattern). Default implementation returns empty results until scope rows exist in PostgreSQL.
/// </summary>
public interface IApiKeyEntityScopeQueryService {
  Result<List<ApiKeyEntityScopeQueryResult>?> Search(
    Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit);

  Result<int?> Count(Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate);
}
