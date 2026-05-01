using System.Linq.Expressions;
using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

/// <summary>
/// Linq2Db-based implementation of <see cref="IUserQueryService"/> (Vault-style predicates on <see cref="UserDto"/>).
/// </summary>
public class UserQueryServiceLinq2Db(ILogger<UserQueryServiceLinq2Db> logger, ICertsDataConnectionFactory connectionFactory) : IUserQueryService {
  private readonly ILogger<UserQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<List<UserQueryResult>?> Search(
    Expression<Func<UserDto, bool>>? usersPredicate,
    int? skip,
    int? limit) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<UserDto>().AsQueryable();
      if (usersPredicate != null)
        query = query.Where(usersPredicate);

      query = query.OrderBy(u => u.Name);

      if (skip.HasValue)
        query = query.Skip(skip.Value);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      var rows = query.ToList();

      var userIds = rows.Select(r => r.Id).ToList();
      var allRc = userIds.Count == 0
        ? []
        : db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => userIds.Contains(t.UserId)).ToList();
      var recoveryCountByUser = allRc
        .GroupBy(t => t.UserId)
        .ToDictionary(g => g.Key, g => g.Count());

      var results = rows.Select(r => MapToQueryResult(r, recoveryCountByUser.GetValueOrDefault(r.Id))).ToList();
      return Result<List<UserQueryResult>?>.Ok(results);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching users.");
      return Result<List<UserQueryResult>?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  public Result<int?> Count(Expression<Func<UserDto, bool>>? usersPredicate) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<UserDto>().AsQueryable();
      if (usersPredicate != null)
        query = query.Where(usersPredicate);

      return Result<int?>.Ok(query.Count());
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while counting users.");
      return Result<int?>.InternalServerError(null, [.. ex.ExtractMessages()]);
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
