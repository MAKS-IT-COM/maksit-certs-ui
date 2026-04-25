using LinqToDB;
using LinqToDB.Data;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Query;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

/// <summary>
/// Linq2Db-based implementation of <see cref="IUserQueryService"/>.
/// </summary>
public class UserQueryServiceLinq2Db(ILogger<UserQueryServiceLinq2Db> logger, ICertsDataConnectionFactory connectionFactory) : IUserQueryService {
  private readonly ILogger<UserQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

  public Task<Result<PagedQueryResult<UserQueryResult>>> SearchUsersAsync(
    string? usernameFilter,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default) {
    _ = cancellationToken;
    try {
      var page = Math.Max(1, pageNumber);
      var size = Math.Clamp(pageSize, 1, 500);
      var skip = (page - 1) * size;
      var filter = usernameFilter?.Trim();

      using var db = _connectionFactory.Create();
      var table = db.GetTable<UserDto>();
      var filtered = string.IsNullOrWhiteSpace(filter)
        ? table
        : table.Where(u => u.Name.Contains(filter!));

      var total = filtered.Count();
      var rows = filtered
        .OrderBy(u => u.Name)
        .Skip(skip)
        .Take(size)
        .ToList();

      var userIds = rows.Select(r => r.Id).ToList();
      var allRc = userIds.Count == 0
        ? []
        : db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => userIds.Contains(t.UserId)).ToList();
      var recoveryCountByUser = allRc
        .GroupBy(t => t.UserId)
        .ToDictionary(g => g.Key, g => g.Count());

      var data = rows.Select(r => MapToQueryResult(r, recoveryCountByUser.GetValueOrDefault(r.Id))).ToList();

      return Task.FromResult(Result<PagedQueryResult<UserQueryResult>>.Ok(new PagedQueryResult<UserQueryResult>(
        data,
        total,
        page,
        size
      )));
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching users.");
      return Task.FromResult(Result<PagedQueryResult<UserQueryResult>>.InternalServerError(null, [.. ex.ExtractMessages()]));
    }
  }

  private static UserQueryResult MapToQueryResult(UserDto row, int recoveryCount) => new() {
    Id = row.Id,
    Username = row.Name,
    IsActive = row.IsActive,
    TwoFactorEnabled = row.TwoFactorSharedKey != null && recoveryCount > 0,
    LastLogin = row.LastLoginUtc == default ? null : row.LastLoginUtc,
  };
}
