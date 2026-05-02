using Microsoft.Extensions.Logging;
using LinqToDB;
using LinqToDB.Data;
using MaksIT.Results;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;


namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IRegistrationCachePersistenceService"/> for PostgreSQL.
/// </summary>
public sealed class RegistrationCachePersistenceServiceLinq2Db(
  ILogger<RegistrationCachePersistenceServiceLinq2Db> logger,
  ICertsUIDataConnectionFactory connectionFactory
) : IRegistrationCachePersistenceService
{

  private readonly ILogger<RegistrationCachePersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Task<Result<RegistrationCache[]?>> LoadAllAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    try
    {
      using var db = _connectionFactory.Create();
      var rows = db.GetTable<RegistrationCacheDto>().ToList();

      var caches = new List<RegistrationCache>();
      foreach (var row in rows)
      {
        if (string.IsNullOrWhiteSpace(row.PayloadJson))
        {
          _logger.LogWarning("Registration cache row is empty for account {AccountId}", row.Id);
          continue;
        }

        var cache = row.PayloadJson.ToObject<RegistrationCache>();
        if (cache == null)
        {
          _logger.LogWarning("Could not deserialize registration cache for account {AccountId}", row.Id);
          continue;
        }

        cache.ConcurrencyVersion = row.Version;
        caches.Add(cache);
      }

      return Task.FromResult(Result<RegistrationCache[]?>.Ok(caches.ToArray()));
    }
    catch (Exception ex)
    {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error loading all registration caches.");
      return Task.FromResult(Result<RegistrationCache[]?>.InternalServerError(null, ["An error occurred while loading registration caches.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<RegistrationCache?>> LoadAsync(Guid accountId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    try
    {
      using var db = _connectionFactory.Create();
      var row = db.GetTable<RegistrationCacheDto>().FirstOrDefault(r => r.Id == accountId);
      if (row == null)
        return Task.FromResult(Result<RegistrationCache?>.NotFound(null, $"Registration cache not found for account {accountId}."));

      if (string.IsNullOrWhiteSpace(row.PayloadJson))
        return Task.FromResult(Result<RegistrationCache?>.InternalServerError(null, $"Registration cache payload is empty for account {accountId}."));

      var cache = row.PayloadJson.ToObject<RegistrationCache>();
      if (cache == null)
        return Task.FromResult(Result<RegistrationCache?>.InternalServerError(null, $"Registration cache payload is invalid for account {accountId}."));

      cache.ConcurrencyVersion = row.Version;
      return Task.FromResult(Result<RegistrationCache?>.Ok(cache));
    }
    catch (Exception ex)
    {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error loading registration cache for account {AccountId}", accountId);
      return Task.FromResult(Result<RegistrationCache?>.InternalServerError(null, ["An error occurred while loading the registration cache.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> SaveAsync(Guid accountId, RegistrationCache cache, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(cache);

    try
    {
      using var db = _connectionFactory.Create();
      cache.AccountId = accountId;
      var json = cache.ToJson();
      var row = db.GetTable<RegistrationCacheDto>().FirstOrDefault(r => r.Id == accountId);

      if (row == null)
      {
        db.Insert(new RegistrationCacheDto
        {
          Id = accountId,
          Version = 1,
          PayloadJson = json
        });
        cache.ConcurrencyVersion = 1;
      }
      else
      {
        var expectedVersion = cache.ConcurrencyVersion > 0 ? cache.ConcurrencyVersion : row.Version;
        var nextVersion = expectedVersion + 1;

        var updated = db.GetTable<RegistrationCacheDto>()
          .Where(r => r.Id == accountId && r.Version == expectedVersion)
          .Set(r => r.PayloadJson, json)
          .Set(r => r.Version, nextVersion)
          .Update();

        if (updated == 0)
        {
          _logger.LogWarning(
            "Optimistic concurrency conflict for registration cache {AccountId}. Expected version {ExpectedVersion}.",
            accountId, expectedVersion);
          return Task.FromResult(Result.Conflict($"Registration cache was modified concurrently for account {accountId}. Reload and retry."));
        }

        cache.ConcurrencyVersion = nextVersion;
      }

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex)
    {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error saving registration cache for account {AccountId}", accountId);
      return Task.FromResult(Result.InternalServerError(["An error occurred while saving the registration cache.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> DeleteAllAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    try
    {
      using var db = _connectionFactory.Create();
      db.Execute("DELETE FROM registration_caches");
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex)
    {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting all registration caches.");
      return Task.FromResult(Result.InternalServerError(["An error occurred while deleting registration caches.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    try
    {
      using var db = _connectionFactory.Create();
      var deleted = db.GetTable<RegistrationCacheDto>().Where(r => r.Id == accountId).Delete();
      if (deleted == 0)
      {
        _logger.LogWarning("Registration cache not found for account {AccountId}", accountId);
        return Task.FromResult(Result.Ok());
      }

      _logger.LogInformation("Registration cache deleted for account {AccountId}", accountId);
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex)
    {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting registration cache for account {AccountId}", accountId);
      return Task.FromResult(Result.InternalServerError(["An error occurred while deleting the registration cache.", .. ex.ExtractMessages()]));
    }
  }

}
