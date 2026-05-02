using System.Linq.Expressions;
using MaksIT.Core.Webapi.Models;

namespace MaksIT.CertsUI.Models.Identity.User.Search;

public class SearchUserRequest : PagedRequest {
  public string? OrganizationFilters { get; set; }

  public string? ApplicationFilters { get; set; }

  public Expression<Func<T, bool>> BuildOrganizationFilterExpression<T>() {
    return BuildFilterExpression<T>(OrganizationFilters);
  }

  public Expression<Func<T, bool>> BuildApplicationFilterExpression<T>() {
    return BuildFilterExpression<T>(ApplicationFilters);
  }
}
