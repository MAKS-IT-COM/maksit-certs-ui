using System.Text.Json;

using DomainResults.Common;

using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<(RegistrationCache[]?, IDomainResult)> LoadAccountsFromCacheAsync();
  Task<(RegistrationCache?, IDomainResult)> LoadAccountFromCacheAsync(Guid accountId);
  Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<IDomainResult> DeleteFromCacheAsync(Guid accountId);
}

public class CacheService : ICacheService, IDisposable {
  private readonly ILogger<CacheService> _logger;
  private readonly string _cacheDirectory;
  private readonly LockManager _lockManager;

  public CacheService(ILogger<CacheService> logger) {
    _logger = logger;
    _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
    _lockManager = new LockManager();

    if (!Directory.Exists(_cacheDirectory)) {
      Directory.CreateDirectory(_cacheDirectory);
    }
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

  public async Task<(RegistrationCache[]?, IDomainResult)> LoadAccountsFromCacheAsync() {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var accountIds = GetCachedAccounts();
      var cacheLoadTasks = accountIds.Select(accountId => LoadFromCacheInternalAsync(accountId)).ToList();

      var caches = new List<RegistrationCache>();
      foreach (var task in cacheLoadTasks) {
        var (registrationCache, getRegistrationCacheResult) = await task;
        if (!getRegistrationCacheResult.IsSuccess || registrationCache == null) {
          // Depending on how you want to handle partial failures, you might want to return here
          // or continue loading other caches. For now, let's continue.
          continue;
        }

        caches.Add(registrationCache);
      }

      return IDomainResult.Success(caches.ToArray());
    });
  }




  private async Task<(RegistrationCache?, IDomainResult)> LoadFromCacheInternalAsync(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);

    if (!File.Exists(cacheFilePath)) {
      var message = $"Cache file not found for account {accountId}";
      _logger.LogWarning(message);
      return IDomainResult.Failed<RegistrationCache>(message);
    }

    var json = await File.ReadAllTextAsync(cacheFilePath);
    if (string.IsNullOrEmpty(json)) {
      var message = $"Cache file is empty for account {accountId}";
      _logger.LogWarning(message);
      return IDomainResult.Failed<RegistrationCache>(message);
    }

    var cache = JsonSerializer.Deserialize<RegistrationCache>(json);
    return IDomainResult.Success(cache);
  }



  private async Task<IDomainResult> SaveToCacheInternalAsync(Guid accountId, RegistrationCache cache) {
    var cacheFilePath = GetCacheFilePath(accountId);
    var json = JsonSerializer.Serialize(cache);
    await File.WriteAllTextAsync(cacheFilePath, json);
    _logger.LogInformation($"Cache file saved for account {accountId}");
    return DomainResult.Success();
  }



  private IDomainResult DeleteFromCacheInternal(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);
    if (File.Exists(cacheFilePath)) {
      File.Delete(cacheFilePath);
      _logger.LogInformation($"Cache file deleted for account {accountId}");
    }
    else {
      _logger.LogWarning($"Cache file not found for account {accountId}");
    }
    return DomainResult.Success();
  }

  #endregion


  public Task<(RegistrationCache?, IDomainResult)> LoadAccountFromCacheAsync(Guid accountId) {
    return _lockManager.ExecuteWithLockAsync(() => LoadFromCacheInternalAsync(accountId));
  }

  public Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    return _lockManager.ExecuteWithLockAsync(() => SaveToCacheInternalAsync(accountId, cache));
  }

  public Task<IDomainResult> DeleteFromCacheAsync(Guid accountId) {
    return _lockManager.ExecuteWithLockAsync(() => DeleteFromCacheInternal(accountId));
  }

  public void Dispose() {
    _lockManager.Dispose();
  }
}
