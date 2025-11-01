using System.Text.Json;


using MaksIT.Core.Extensions;
using MaksIT.Core.Threading;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Results;
using Microsoft.Extensions.Options;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync();
  Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId);
  Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<Result> DeleteFromCacheAsync(Guid accountId);
}

public class CacheService : ICacheService, IDisposable {
  private readonly ILogger<CacheService> _logger;
  private readonly string _cacheDirectory;
  private readonly LockManager _lockManager;

  public CacheService(
    ILogger<CacheService> logger,
    IOptions<Configuration> appsettings
  ) {
    _logger = logger;
    _cacheDirectory = appsettings.Value.CacheFolder;
    _lockManager = new LockManager();
  }

  /// <summary>
  /// Generates the cache file path for the given account ID.
  /// </summary>
  private string GetCacheFilePath(Guid accountId) {
    return Path.Combine(_cacheDirectory, $"{accountId}.json");
  }

  private Guid[] GetCachedAccounts() {
    return GetCacheFilesPaths().Select(x => Path.GetFileNameWithoutExtension(x).ToGuid()).Where(x => x != Guid.Empty).ToArray();
  }

  private string[] GetCacheFilesPaths() {
    return Directory.GetFiles(_cacheDirectory);
  }

  #region Cache Operations

  public async Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var accountIds = GetCachedAccounts();
      var cacheLoadTasks = accountIds.Select(accountId => LoadFromCacheInternalAsync(accountId)).ToList();

      var caches = new List<RegistrationCache>();
      foreach (var task in cacheLoadTasks) {
        var taskResult = await task;
        if (!taskResult.IsSuccess || taskResult.Value == null) {
          // Depending on how you want to handle partial failures, you might want to return here
          // or continue loading other caches. For now, let's continue.
          continue;
        }

        var registrationCache = taskResult.Value;

        caches.Add(registrationCache);
      }

      return Result<RegistrationCache[]?>.Ok(caches.ToArray());
    });
  }

  private async Task<Result<RegistrationCache?>> LoadFromCacheInternalAsync(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);

    if (!File.Exists(cacheFilePath)) {
      var message = $"Cache file not found for account {accountId}";
      _logger.LogWarning(message);
      return Result<RegistrationCache?>.InternalServerError(null, message);
    }

    var json = await File.ReadAllTextAsync(cacheFilePath);
    if (string.IsNullOrEmpty(json)) {
      var message = $"Cache file is empty for account {accountId}";
      _logger.LogWarning(message);
      return Result<RegistrationCache?>.InternalServerError(null, message);
    }

    var cache = JsonSerializer.Deserialize<RegistrationCache>(json);
    return Result<RegistrationCache?>.Ok(cache);
  }

  private async Task<Result> SaveToCacheInternalAsync(Guid accountId, RegistrationCache cache) {
    var cacheFilePath = GetCacheFilePath(accountId);
    var json = JsonSerializer.Serialize(cache);
    await File.WriteAllTextAsync(cacheFilePath, json);
    _logger.LogInformation($"Cache file saved for account {accountId}");
    return Result.Ok();
  }

  private Result DeleteFromCacheInternal(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);
    if (File.Exists(cacheFilePath)) {
      File.Delete(cacheFilePath);
      _logger.LogInformation($"Cache file deleted for account {accountId}");
    }
    else {
      _logger.LogWarning($"Cache file not found for account {accountId}");
    }
    return Result.Ok();
  }

  #endregion

  public async Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(() => LoadFromCacheInternalAsync(accountId));

  }

  public async Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    return await _lockManager.ExecuteWithLockAsync(() => SaveToCacheInternalAsync(accountId, cache));
  }

  public async Task<Result> DeleteFromCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(() => DeleteFromCacheInternal(accountId));
  }

  public void Dispose() {
    _lockManager.Dispose();
  }
}
