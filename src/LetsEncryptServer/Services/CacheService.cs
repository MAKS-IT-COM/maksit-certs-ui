using System.Text.Json;

using DomainResults.Common;
using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<(RegistrationCache?, IDomainResult)> LoadFromCacheAsync(Guid accountId);
  Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<IDomainResult> DeleteFromCacheAsync(Guid accountId);
  Task<(Guid[]?, IDomainResult)> ListCachedAccountsAsync();
}

public class CacheService : ICacheService, IDisposable {

  private readonly ILogger<CacheService> _logger;
  private readonly string _cacheDirectory;
  private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

  public CacheService(
    ILogger<CacheService> logger
  ) {
    _logger = logger;
    _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");

    if (!Directory.Exists(_cacheDirectory)) {
      Directory.CreateDirectory(_cacheDirectory);
    }
  }

  private string GetCacheFilePath(Guid accountId) {
    return Path.Combine(_cacheDirectory, $"{accountId}.json");
  }

  public async Task<(RegistrationCache?, IDomainResult)> LoadFromCacheAsync(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);

    await _cacheLock.WaitAsync();

    try {
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
    catch (Exception ex) {
      var message = "Error reading cache file for account {accountId}";
      _logger.LogError(ex, message);

      return IDomainResult.Failed<RegistrationCache?>(message);
    }
    finally {
      _cacheLock.Release();
    }
  }

  public async Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    var cacheFilePath = GetCacheFilePath(accountId);
    await _cacheLock.WaitAsync();

    try {
      var json = JsonSerializer.Serialize(cache);
      await File.WriteAllTextAsync(cacheFilePath, json);

      _logger.LogInformation($"Cache file saved for account {accountId}");

      return DomainResult.Success();
    }
    catch (Exception ex) {
      var message = "Error writing cache file for account {accountId}";
      _logger.LogError(ex, message);
      
      return IDomainResult.Failed(message);
    }
    finally {
      _cacheLock.Release();
    }
  }

  public async Task<IDomainResult> DeleteFromCacheAsync(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);
    await _cacheLock.WaitAsync();

    try {
      if (File.Exists(cacheFilePath)) {
        File.Delete(cacheFilePath);
        _logger.LogInformation($"Cache file deleted for account {accountId}");
      }
      else {
        _logger.LogWarning($"Cache file not found for account {accountId}");
      }

      return IDomainResult.Success();
    }
    catch (Exception ex) {
      var message = $"Error deleting cache file for account {accountId}";
      _logger.LogError(ex, message);

      return IDomainResult.Failed(message);
    }
    finally {
      _cacheLock.Release();
    }
  }

  public async Task<(Guid[]?, IDomainResult)> ListCachedAccountsAsync() {
    await _cacheLock.WaitAsync();

    try {
      var cacheFiles = Directory.GetFiles(_cacheDirectory);
      if (cacheFiles == null)
        return IDomainResult.Success(new Guid[0]);

      var accountIds = cacheFiles.Select(x => Path.GetFileNameWithoutExtension(x).ToGuid()).ToArray();

      return IDomainResult.Success(accountIds);
    }
    catch (Exception ex) {
      var message = "Error listing cache files";
      _logger.LogError(ex, message);

      return IDomainResult.Failed<Guid[]?> (message);
    }
    finally {
      _cacheLock.Release();
    }
  }

  public void Dispose() {
    _cacheLock?.Dispose();
  }
}
