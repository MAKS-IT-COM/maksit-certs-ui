using MaksIT.CertsUI.Engine.Query;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.QueryServices.Identity;

/// <summary>
/// Read-only paged user search for admin/list views.
/// </summary>
public interface IUserQueryService {

  Task<Result<PagedQueryResult<UserQueryResult>>> SearchUsersAsync(
    string? usernameFilter,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default);
}
