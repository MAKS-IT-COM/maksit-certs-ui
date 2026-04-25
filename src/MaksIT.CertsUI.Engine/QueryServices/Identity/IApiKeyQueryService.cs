using MaksIT.CertsUI.Engine.Query;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// Read-only paged API key search for list views.
/// </summary>
public interface IApiKeyQueryService {

  Task<Result<PagedQueryResult<ApiKeyQueryResult>>> SearchApiKeysAsync(
    string? descriptionFilter,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default);
}
