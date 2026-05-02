using System.Linq.Expressions;
using MaksIT.Core.Webapi.Models;

namespace MaksIT.CertsUI.Models.APIKeys.Search;

public class SearchAPIKeyRequest : PagedRequest {

  public string? OrganizationFilters { get; set; }

  public string? ApplicationFilters { get; set; }

  public Expression<Func<T, bool>> BuildOrganizationFilterExpression<T>() {
    return BuildFilterExpression<T>(OrganizationFilters);
  }

  public Expression<Func<T, bool>> BuildApplicationFilterExpression<T>() {
    return BuildFilterExpression<T>(ApplicationFilters);
  }
}
