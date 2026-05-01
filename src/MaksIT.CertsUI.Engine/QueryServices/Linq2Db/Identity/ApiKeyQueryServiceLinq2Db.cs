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
/// Linq2Db-based implementation of <see cref="IApiKeyQueryService"/> (Vault-style predicates on <see cref="ApiKeyDto"/>).
/// </summary>
public class ApiKeyQueryServiceLinq2Db(ILogger<ApiKeyQueryServiceLinq2Db> logger, ICertsDataConnectionFactory connectionFactory) : IApiKeyQueryService {
  private readonly ILogger<ApiKeyQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<List<ApiKeyQueryResult>?> Search(
    Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate,
    int? skip,
    int? limit) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<ApiKeyDto>().AsQueryable();
      if (apiKeysPredicate != null)
        query = query.Where(apiKeysPredicate);

      query = query.OrderByDescending(k => k.CreatedAtUtc);

      if (skip.HasValue)
        query = query.Skip(skip.Value);

      if (limit.HasValue)
        query = query.Take(limit.Value);

      var rows = query.ToList();
      var results = rows.Select(MapToQueryResult).ToList();
      return Result<List<ApiKeyQueryResult>?>.Ok(results);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching API keys.");
      return Result<List<ApiKeyQueryResult>?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  public Result<int?> Count(Expression<Func<ApiKeyDto, bool>>? apiKeysPredicate) {
    try {
      using var db = _connectionFactory.Create();
      var query = db.GetTable<ApiKeyDto>().AsQueryable();
      if (apiKeysPredicate != null)
        query = query.Where(apiKeysPredicate);

      return Result<int?>.Ok(query.Count());
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while counting API keys.");
      return Result<int?>.InternalServerError(null, [.. ex.ExtractMessages()]);
    }
  }

  private static ApiKeyQueryResult MapToQueryResult(ApiKeyDto dto) => new() {
    Id = dto.Id,
    Description = dto.Description,
    CreatedAt = dto.CreatedAtUtc,
    ExpiresAt = dto.ExpiresAtUtc,
    RevokedAt = dto.RevokedAtUtc,
  };
}
