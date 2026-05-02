using System.Linq.Expressions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

public interface IUserEntityScopeQueryService {
  Result<List<UserEntityScopeQueryResult>?> Search(
    Expression<Func<UserEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit);
  Result<int?> Count(Expression<Func<UserEntityScopeDto, bool>>? predicate);
}
