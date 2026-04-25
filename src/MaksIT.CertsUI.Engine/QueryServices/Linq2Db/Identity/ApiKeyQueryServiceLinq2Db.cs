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
/// Linq2Db-based implementation of <see cref="IApiKeyQueryService"/>.
/// </summary>
public class ApiKeyQueryServiceLinq2Db(ILogger<ApiKeyQueryServiceLinq2Db> logger, ICertsDataConnectionFactory connectionFactory) : IApiKeyQueryService {
  private readonly ILogger<ApiKeyQueryServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

  public Task<Result<PagedQueryResult<ApiKeyQueryResult>>> SearchApiKeysAsync(
    string? descriptionFilter,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default) {
    _ = cancellationToken;
    try {
      var page = Math.Max(1, pageNumber);
      var size = Math.Clamp(pageSize, 1, 500);
      var skip = (page - 1) * size;
      var filter = descriptionFilter?.Trim();

      using var db = _connectionFactory.Create();
      var table = db.GetTable<ApiKeyDto>();
      var filtered = string.IsNullOrWhiteSpace(filter)
        ? table
        : table.Where(k => (k.Description ?? string.Empty).Contains(filter));

      var total = filtered.Count();
      var rows = filtered
        .OrderByDescending(k => k.CreatedAtUtc)
        .Skip(skip)
        .Take(size)
        .ToList();

      var data = rows.Select(MapToQueryResult).ToList();

      return Task.FromResult(Result<PagedQueryResult<ApiKeyQueryResult>>.Ok(new PagedQueryResult<ApiKeyQueryResult>(
        data,
        total,
        page,
        size
      )));
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error occurred while searching API keys.");
      return Task.FromResult(Result<PagedQueryResult<ApiKeyQueryResult>>.InternalServerError(null, [.. ex.ExtractMessages()]));
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
