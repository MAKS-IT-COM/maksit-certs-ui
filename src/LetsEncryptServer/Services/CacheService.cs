using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using DomainResults.Common;
using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;
using Models.LetsEncryptServer.Cache.Responses;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<(RegistrationCache?, IDomainResult)> LoadFromCacheAsync(Guid accountId);
  Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<IDomainResult> DeleteFromCacheAsync(Guid accountId);
  Task<(GetAccountsResponse?, IDomainResult)> GetAccountsAsync();
  Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId);
  Task<IDomainResult> SetContactsAsync(Guid accountId, SetContactsRequest requestData);

  Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId);
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

  public async Task<(GetAccountsResponse?, IDomainResult)> GetAccountsAsync() {
    await _cacheLock.WaitAsync();

    try {
      var cacheFiles = Directory.GetFiles(_cacheDirectory);
      if (cacheFiles == null)
        return IDomainResult.Success(new GetAccountsResponse {
          AccountIds = Array.Empty<Guid>()
        });

      var accountIds = cacheFiles.Select(x => Path.GetFileNameWithoutExtension(x).ToGuid()).ToArray();

      return IDomainResult.Success(new GetAccountsResponse {
        AccountIds = accountIds
      });
    }
    catch (Exception ex) {
      var message = "Error listing cache files";
      _logger.LogError(ex, message);

      return IDomainResult.Failed<GetAccountsResponse?> (message);
    }
    finally {
      _cacheLock.Release();
    }
  }

  public async Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return (null, loadResult);

    return IDomainResult.Success(new GetContactsResponse {
      Contacts = cache.Contacts ?? Array.Empty<string>()
    });
  }


  public async Task<IDomainResult> SetContactsAsync(Guid accountId, SetContactsRequest requestData) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return loadResult;

    cache.Contacts = requestData.Contacts;
    return await SaveToCacheAsync(accountId, cache);
  }

  public async Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache?.CachedCerts == null)
      return (null, loadResult);

    var hoststWithUpcomingSslExpire = cache.GetHostsWithUpcomingSslExpiry();


    var response = new GetHostnamesResponse {
      Hostnames = new List<HostnameResponse>()
    };

    foreach (var result in cache.CachedCerts) {
      var (subject, cachedChert) = result;

      var cert = new X509Certificate2(Encoding.ASCII.GetBytes(cachedChert.Cert));

      response.Hostnames.Add(new HostnameResponse {
        Hostname = subject,
        Expires = cert.NotBefore,
        IsUpcomingExpire = hoststWithUpcomingSslExpire.Contains(subject)
      });
    }

    return IDomainResult.Success(response);
  }


  public void Dispose() {
    _cacheLock?.Dispose();
  }
}
