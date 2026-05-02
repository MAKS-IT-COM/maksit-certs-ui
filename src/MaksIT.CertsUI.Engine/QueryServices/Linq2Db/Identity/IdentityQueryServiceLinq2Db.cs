using System.Linq.Expressions;
using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

/// <summary>
/// Linq2Db-based implementation of <see cref="IIdentityQueryService"/>.
/// </summary>
public class IdentityQueryServiceLinq2Db(ILogger<IdentityQueryServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IIdentityQueryService {
  private readonly ILogger<IdentityQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<List<UserQueryResult>?> Search(
    Expression<Func<UserDto, bool>>? usersPredicate,
    int? skip,
    int? limit
  ) {
    try {
      using var db = _connectionFactory
        .Create();

      var query = db.GetTable<UserDto>()
        .AsQueryable();

      if (usersPredicate != null)
        query = query.Where(usersPredicate);

      if (skip.HasValue)
        query = query.Skip(skip.Value);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      var usersDtos = query.ToList();

      var userIds = usersDtos.Select(u => u.Id)
        .ToList();

      var recoveryCodes = db.GetTable<TwoFactorRecoveryCodeDto>()
        .Where(t => userIds.Contains(t.UserId))
        .ToList();

      var recoveryByUser = recoveryCodes
        .GroupBy(t => t.UserId)
        .ToDictionary(g => g.Key, g => g.ToList());

      foreach (var u in usersDtos)
        u.TwoFactorRecoveryCodes = recoveryByUser.GetValueOrDefault(u.Id, []);

      var results = usersDtos.Select(MapToQueryResult).ToList();
      return Result<List<UserQueryResult>?>.Ok(results);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error occurred while searching users. Params: {UsersPredicate}, {Skip}, {Limit}", usersPredicate, skip, limit);
      return Result<List<UserQueryResult>?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  public Result<int?> Count(Expression<Func<UserDto, bool>>? usersPredicate) {
    try {
      using var db = _connectionFactory.Create();

      var query = db.GetTable<UserDto>().AsQueryable();
      if (usersPredicate != null)
        query = query.Where(usersPredicate);

      var count = query.Count();
      return Result<int?>.Ok(count);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error occurred while counting users. Params: {UsersPredicate}", usersPredicate);
      return Result<int?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  private static UserQueryResult MapToQueryResult(UserDto userDto) {
    return new() {
      Id = userDto.Id,
      Username = userDto.Username,
      Email = userDto.Email,
      MobileNumber = userDto.MobileNumber,
      IsActive = userDto.IsActive,
      TwoFactorEnabled = userDto.TwoFactorSharedKey != null && userDto.TwoFactorRecoveryCodes.Count != 0,
      RecoveryCodesLeft = userDto.TwoFactorRecoveryCodes.Count(x => !x.IsUsed),
      CreatedAt = userDto.CreatedAt,
      LastLogin = userDto.LastLogin,
      IsGlobalAdmin = userDto.IsGlobalAdmin
    };
  }
}
