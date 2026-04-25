using MaksIT.CertsUI.Engine.Query;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// API key ↔ entity scope search. Certs has no persisted scope graph yet; default implementation returns an empty page.
/// </summary>
public interface IApiKeyEntityScopeQueryService {
  Task<Result<PagedQueryResult<ApiKeyEntityScopeQueryResult>>> SearchApiKeyEntityScopesAsync(
    Guid? apiKeyId,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default);
}
