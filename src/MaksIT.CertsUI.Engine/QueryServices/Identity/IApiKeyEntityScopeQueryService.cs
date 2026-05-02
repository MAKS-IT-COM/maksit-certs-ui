using System.Linq.Expressions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

public interface IApiKeyEntityScopeQueryService {
  Result<List<ApiKeyEntityScopeQueryResult>?> Search(
    Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit);
  Result<int?> Count(Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate);
}
