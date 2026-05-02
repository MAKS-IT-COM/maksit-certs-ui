using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using LinqToDB;
using LinqToDB.Data;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;


namespace MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;

public class ApiKeyEntityScopeQueryServiceLinq2Db(ILogger<ApiKeyEntityScopeQueryServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IApiKeyEntityScopeQueryService {
  private readonly ILogger<ApiKeyEntityScopeQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<List<ApiKeyEntityScopeQueryResult>?> Search(
    Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate,
    int? skip,
    int? limit
  ) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<ApiKeyEntityScopeDto>().AsQueryable();
      if (predicate != null)
        query = query.Where(predicate);

      if (skip.HasValue)
        query = query.Skip(skip.Value);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      var dtos = query.ToList();

      var apiKeyIds = dtos.Select(d => d.ApiKeyId).Distinct().ToList();

      var apiKeys = apiKeyIds.Count > 0
        ? db.GetTable<ApiKeyDto>()
            .Where(k => apiKeyIds.Contains(k.Id))
            .Select(k => new { k.Id, k.Description })
            .ToDictionary(x => x.Id, x => x.Description ?? "(API Key)")
        : [];

      var nameLookup = BuildEntityNameLookup(db, dtos);

      var results = dtos.Select(dto => MapToQueryResult(dto, apiKeys, nameLookup)).ToList();

      return Result<List<ApiKeyEntityScopeQueryResult>?>.Ok(results);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching API key entity scopes.");
      return Result<List<ApiKeyEntityScopeQueryResult>?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  public Result<int?> Count(Expression<Func<ApiKeyEntityScopeDto, bool>>? predicate) {
    try {
      using var db = _connectionFactory.Create();

      var query = db.GetTable<ApiKeyEntityScopeDto>().AsQueryable();

      if (predicate != null)
        query = query.Where(predicate);

      var count = query.Count();

      return Result<int?>.Ok(count);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while counting API key entity scopes.");
      return Result<int?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  private static Dictionary<ScopeEntityType, Dictionary<Guid, string>> BuildEntityNameLookup(DataConnection db, List<ApiKeyEntityScopeDto> dtos) {
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

  private static ApiKeyEntityScopeQueryResult MapToQueryResult(ApiKeyEntityScopeDto dto, Dictionary<Guid, string> apiKeys, Dictionary<ScopeEntityType, Dictionary<Guid, string>> nameLookup) {
    var entityName = nameLookup.TryGetValue(dto.EntityType, out var dict) && dict.TryGetValue(dto.EntityId, out var name)
      ? name
      : null;

    var description = apiKeys.TryGetValue(dto.ApiKeyId, out var desc) ? desc : null;

    return new ApiKeyEntityScopeQueryResult {
      Id = dto.Id,
      ApiKeyId = dto.ApiKeyId,
      Description = description,
      EntityId = dto.EntityId,
      EntityName = entityName,
      EntityType = dto.EntityType,
      Scope = dto.Scope
    };
  }
}
