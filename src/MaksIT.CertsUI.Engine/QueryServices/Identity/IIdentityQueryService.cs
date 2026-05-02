using System.Linq.Expressions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

public interface IIdentityQueryService {
  Result<List<UserQueryResult>?> Search(
    Expression<Func<UserDto, bool>>? usersPredicate,
    int? skip,
    int? limit);
  Result<int?> Count(Expression<Func<UserDto, bool>>? usersPredicate);
}
