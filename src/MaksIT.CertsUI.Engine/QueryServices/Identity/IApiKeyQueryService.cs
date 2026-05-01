using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// Read-only API key search (Vault <c>IApiKeyQueryService</c> pattern): optional predicate on <see cref="ApiKeyDto"/>, skip/limit, and count.
/// </summary>
public interface IApiKeyQueryService {

  Result<List<ApiKeyQueryResult>?> Search(
    Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate,
    int? skip,
    int? limit);

  Result<int?> Count(Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate);
}
