using System.Linq.Expressions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

public interface IApiKeyQueryService {
  Result<List<ApiKeyQueryResult>?> Search(
    Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate,
    int? skip,
    int? limit);
  Result<int?> Count(Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate);
}
