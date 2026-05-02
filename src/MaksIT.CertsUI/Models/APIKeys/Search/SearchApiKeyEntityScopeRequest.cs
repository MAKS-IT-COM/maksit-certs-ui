using System.Linq.Expressions;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Models.APIKeys.Search;

public class SearchApiKeyEntityScopeRequest : PagedRequest {
  /// <summary>
  /// Optional filter by parent API key ID.
  /// </summary>
  public Guid? ApiKeyId { get; set; }

  public Expression<Func<ApiKeyEntityScopeDto, bool>>? BuildFilterExpression() =>
    BuildFilterExpression<ApiKeyEntityScopeDto>(Filters);
}
