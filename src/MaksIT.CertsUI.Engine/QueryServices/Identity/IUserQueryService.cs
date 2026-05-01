using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// Read-only user search (MaksIT.Vault.Engine.QueryServices.Identity.IIdentityQueryService pattern): optional Linq2Db-translatable predicate on <see cref="UserDto"/>, skip/limit, and a separate count.
/// Host builds optional <see cref="System.Linq.Expressions.Expression"/> predicates on <see cref="UserDto"/> for filters and RBAC; use <see cref="MaksIT.CertsUI.Engine.QueryServices.ExpressionCompose"/> to compose nested predicates (Vault parity).
/// </summary>
public interface IUserQueryService {

  Result<List<UserQueryResult>?> Search(
    Expression<Func<UserDto, bool>>? usersPredicate,
    int? skip,
    int? limit);

  Result<int?> Count(Expression<Func<UserDto, bool>>? usersPredicate);
}
