using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

public class UserEntityScopeQueryServiceLinq2Db(ILogger<UserEntityScopeQueryServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IUserEntityScopeQueryService {
  private readonly ILogger<UserEntityScopeQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<List<UserEntityScopeQueryResult>?> Search(
    Expression<Func<UserEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<UserEntityScopeDto>().AsQueryable();

      if (predicate != null)
        query = query.Where(predicate);

      if (skip.HasValue)
        query = query.Skip(skip.Value);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      var dtos = query.ToList();

      var userIds = dtos.Select(d => d.UserId).Distinct().ToList();

      var users = userIds.Count > 0
        ? db.GetTable<UserDto>().Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Username }).ToDictionary(x => x.Id, x => x.Username ?? "")
        : [];

      var nameLookup = BuildEntityNameLookup(db, dtos);

      var results = dtos.Select(dto => MapToQueryResult(dto, users, nameLookup)).ToList();
      return Result<List<UserEntityScopeQueryResult>?>.Ok(results);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching user entity scopes.");
      return Result<List<UserEntityScopeQueryResult>?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  public Result<int?> Count(Expression<Func<UserEntityScopeDto, bool>>? predicate) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<UserEntityScopeDto>().AsQueryable();
      if (predicate != null)
        query = query.Where(predicate);

      var count = query.Count();
      return Result<int?>.Ok(count);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while counting user entity scopes.");
      return Result<int?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  private static Dictionary<ScopeEntityType, Dictionary<Guid, string>> BuildEntityNameLookup(DataConnection db, List<UserEntityScopeDto> dtos) {
    var result = new Dictionary<ScopeEntityType, Dictionary<Guid, string>>();

    foreach (ScopeEntityType entityType in Enum.GetValues<ScopeEntityType>()) {
      var ids = dtos
        .Where(d => d.EntityType == entityType)
        .Select(d => d.EntityId)
        .Distinct()
        .ToList();

      if (ids.Count == 0) {
        result[entityType] = [];
        continue;
      }

      result[entityType] = entityType switch {
        ScopeEntityType.Identity => db.GetTable<UserDto>()
          .Where(u => ids.Contains(u.Id))
          .Select(u => new { u.Id, u.Username })
          .ToDictionary(x => x.Id, x => x.Username ?? ""),
        ScopeEntityType.ApiKey => db.GetTable<ApiKeyDto>()
          .Where(k => ids.Contains(k.Id))
          .Select(k => new { k.Id, k.Description })
          .ToDictionary(x => x.Id, x => x.Description ?? "(API Key)"),
        _ => [],
      };
    }

    return result;
  }

  private static UserEntityScopeQueryResult MapToQueryResult(UserEntityScopeDto dto, Dictionary<Guid, string> users, Dictionary<ScopeEntityType, Dictionary<Guid, string>> nameLookup) {
    var entityName = nameLookup.TryGetValue(dto.EntityType, out var dict) && dict.TryGetValue(dto.EntityId, out var name) ? name : null;
    var username = users.TryGetValue(dto.UserId, out var u)
      ? u
      : null;

    return new UserEntityScopeQueryResult {
      Id = dto.Id,
      UserId = dto.UserId,
      Username = username,
      EntityId = dto.EntityId,
      EntityName = entityName,
      EntityType = dto.EntityType,
      Scope = dto.Scope
    };
  }
}
