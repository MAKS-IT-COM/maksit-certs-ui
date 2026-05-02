using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

public sealed class TermsOfServiceCachePersistenceServiceLinq2Db(
  ILogger<TermsOfServiceCachePersistenceServiceLinq2Db> logger,
  ICertsUIDataConnectionFactory connectionFactory
) : ITermsOfServiceCachePersistenceService {

  public Task<Result<TermsOfServiceCacheDto?>> GetByUrlAsync(string url, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    if (string.IsNullOrWhiteSpace(url))
      return Task.FromResult(Result<TermsOfServiceCacheDto?>.BadRequest(null, "Terms of Service URL is required."));

    try {
      using var db = connectionFactory.Create();
      var row = db.GetTable<TermsOfServiceCacheDto>().FirstOrDefault(x => x.Url == url);
      if (row == null)
        return Task.FromResult(Result<TermsOfServiceCacheDto?>.NotFound(null, $"Terms of Service cache not found for URL: {url}"));

      return Task.FromResult(Result<TermsOfServiceCacheDto?>.Ok(row));
    }
    catch (Exception ex) {
      logger.LogError(ex, "Failed to load Terms of Service cache for {Url}", url);
      return Task.FromResult(Result<TermsOfServiceCacheDto?>.InternalServerError(null, ["Failed to load Terms of Service cache.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> UpsertAsync(TermsOfServiceCacheDto cacheEntry, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(cacheEntry);
    if (string.IsNullOrWhiteSpace(cacheEntry.Url))
      return Task.FromResult(Result.BadRequest("Terms of Service URL is required."));

    try {
      using var db = connectionFactory.Create();
      db.GetTable<TermsOfServiceCacheDto>().InsertOrUpdate(
        () => new TermsOfServiceCacheDto {
          Url = cacheEntry.Url,
          UrlHashHex = cacheEntry.UrlHashHex,
          ETag = cacheEntry.ETag,
          LastModifiedUtc = cacheEntry.LastModifiedUtc,
          ContentType = cacheEntry.ContentType,
          ContentBytes = cacheEntry.ContentBytes,
          FetchedAtUtc = cacheEntry.FetchedAtUtc,
          ExpiresAtUtc = cacheEntry.ExpiresAtUtc
        },
        old => new TermsOfServiceCacheDto {
          Url = old.Url,
          UrlHashHex = cacheEntry.UrlHashHex,
          ETag = cacheEntry.ETag,
          LastModifiedUtc = cacheEntry.LastModifiedUtc,
          ContentType = cacheEntry.ContentType,
          ContentBytes = cacheEntry.ContentBytes,
          FetchedAtUtc = cacheEntry.FetchedAtUtc,
          ExpiresAtUtc = cacheEntry.ExpiresAtUtc
        });

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      logger.LogError(ex, "Failed to upsert Terms of Service cache for {Url}", cacheEntry.Url);
      return Task.FromResult(Result.InternalServerError(["Failed to upsert Terms of Service cache.", .. ex.ExtractMessages()]));
    }
  }
}
