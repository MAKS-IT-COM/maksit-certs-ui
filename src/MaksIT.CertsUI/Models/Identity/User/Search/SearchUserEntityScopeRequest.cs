using System.Linq.Expressions;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Models.Identity.User.Search;

public class SearchUserEntityScopeRequest : PagedRequest {
  /// <summary>
  /// Optional filter by parent user ID.
  /// </summary>
  public Guid? UserId { get; set; }

  public Expression<Func<UserEntityScopeDto, bool>>? BuildFilterExpression() =>
    BuildFilterExpression<UserEntityScopeDto>(Filters);
}
