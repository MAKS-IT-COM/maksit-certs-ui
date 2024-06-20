using System.Text.Json;

using DomainResults.Common;

using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;
using MaksIT.Models.LetsEncryptServer.Cache.Responses;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheInternalsService {
  Task<(RegistrationCache[]?, IDomainResult)> LoadAccountsFromCacheAsync();
  Task<(RegistrationCache?, IDomainResult)> LoadAccountFromCacheAsync(Guid accountId);
  Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<IDomainResult> DeleteFromCacheAsync(Guid accountId);
}

public interface ICacheRestService {
  Task<(GetAccountResponse[]?, IDomainResult)> GetAccountsAsync();
  Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId);
  Task<(GetAccountResponse?, IDomainResult)> PutAccountAsync(Guid accountId, PutAccountRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData);
  Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId);
  Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactsRequest requestData);
  Task<IDomainResult> DeleteContactAsync(Guid accountId, int index);
  Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId);
}

public interface ICacheService : ICacheInternalsService, ICacheRestService {}

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


  public Task<(RegistrationCache?, IDomainResult)> LoadAccountFromCacheAsync(Guid accountId) {
    return _lockManager.ExecuteWithLockAsync(() => LoadFromCacheInternalAsync(accountId));
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

  public Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    return _lockManager.ExecuteWithLockAsync(() => SaveToCacheInternalAsync(accountId, cache));
  }

  private async Task<IDomainResult> SaveToCacheInternalAsync(Guid accountId, RegistrationCache cache) {
    var cacheFilePath = GetCacheFilePath(accountId);
    var json = JsonSerializer.Serialize(cache);
    await File.WriteAllTextAsync(cacheFilePath, json);
    _logger.LogInformation($"Cache file saved for account {accountId}");
    return DomainResult.Success();
  }

  public Task<IDomainResult> DeleteFromCacheAsync(Guid accountId) {
    return _lockManager.ExecuteWithLockAsync(() => DeleteFromCacheInternal(accountId));
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

  #region Account Operations

  public async Task<(GetAccountResponse[]?, IDomainResult)> GetAccountsAsync() {
    return await _lockManager.ExecuteWithLockAsync(async () => {

      var accountIds = GetCachedAccounts();
      var accounts = new List<GetAccountResponse>();

      foreach (var accountId in accountIds) {
        var (account, result) = await GetAccountAsync(accountId);
        if (!result.IsSuccess || account == null) {
          return (null, result);
        }
        accounts.Add(account);
      }

      return IDomainResult.Success(accounts.ToArray());
    });
  }

  public async Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var (cache, result) = await LoadAccountFromCacheAsync(accountId);
      if (!result.IsSuccess || cache == null) {
        return (null, result);
      }

      var response = new GetAccountResponse {
        AccountId = accountId,
        Description = cache.Description,
        Contacts = cache.Contacts,
        Hostnames = GetHostnamesFromCache(cache).ToArray()
      };

      return IDomainResult.Success(response);
    });
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PutAccountAsync(Guid accountId, PutAccountRequest requestData) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    cache.Description = requestData.Description;
    cache.Contacts = requestData.Contacts;

    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return CreateGetAccountResponse(accountId, cache);
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    if (requestData.Description != null) {
      switch (requestData.Description.Op) {
        case PatchOperation.Replace:
          cache.Description = requestData.Description.Value;
          break;
      }
    }

    if (requestData.Contacts != null && requestData.Contacts.Any()) {
      var contacts = cache.Contacts?.ToList() ?? new List<string>();
      foreach (var action in requestData.Contacts) {
        switch (action.Op)
        {
          case PatchOperation.Add:
            if (action.Value != null) contacts.Add(action.Value);
            break;
          case PatchOperation.Replace:
            if (action.Index != null && action.Index >= 0 && action.Index < contacts.Count)
              contacts[action.Index.Value] = action.Value;
            break;
          case PatchOperation.Remove:
            if (action.Index != null && action.Index >= 0 && action.Index < contacts.Count)
              contacts.RemoveAt(action.Index.Value);
            break;
        }
      }
      cache.Contacts = contacts.ToArray();
    }

    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return CreateGetAccountResponse(accountId, cache);
  }

  #endregion

  #region Contacts Operations

  public async Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    return IDomainResult.Success(new GetContactsResponse {
      Contacts = cache.Contacts ?? Array.Empty<string>()
    });
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    cache.Contacts = requestData.Contacts;
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return CreateGetAccountResponse(accountId, cache);
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactsRequest requestData) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    foreach (var contact in requestData.Contacts) {
      switch (contact.Op) {
        case PatchOperation.Add:
          if (contact.Value != null) {
            contacts.Add(contact.Value);
          }
          break;
        case PatchOperation.Replace:
          if (contact.Index.HasValue && contact.Index.Value >= 0 && contact.Index.Value < contacts.Count && contact.Value != null) {
            contacts[contact.Index.Value] = contact.Value;
          }
          break;
        case PatchOperation.Remove:
          if (contact.Index.HasValue && contact.Index.Value >= 0 && contact.Index.Value < contacts.Count) {
            contacts.RemoveAt(contact.Index.Value);
          }
          break;
        default:
          return (null, IDomainResult.Failed("Invalid patch operation."));
      }
    }

    cache.Contacts = contacts.ToArray();
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return CreateGetAccountResponse(accountId, cache);
  }

  public async Task<IDomainResult> DeleteContactAsync(Guid accountId, int index) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return loadResult;
    }

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    if (index >= 0 && index < contacts.Count) {
      contacts.RemoveAt(index);
    }

    cache.Contacts = contacts.ToArray();
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return saveResult;
    }

    return IDomainResult.Success();
  }

  #endregion

  #region Hostnames Operations

  public async Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId) {
    var (cache, loadResult) = await LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache?.CachedCerts == null) {
      return (null, loadResult);
    }

    var hostnames = GetHostnamesFromCache(cache);

    return IDomainResult.Success(new GetHostnamesResponse {
      Hostnames = hostnames
    });
  }

  private List<HostnameResponse> GetHostnamesFromCache(RegistrationCache cache) {
    var hosts = cache.GetHosts().Select(x => new HostnameResponse {
      Hostname = x.Hostname,
      Expires = x.Expires,
      IsUpcomingExpire = x.IsUpcomingExpire
    }).ToList();

    return hosts;
  }

  #endregion

  #region Helper Methods

  private (GetAccountResponse?, IDomainResult) CreateGetAccountResponse(Guid accountId, RegistrationCache cache) {
    var hostnames = GetHostnamesFromCache(cache) ?? new List<HostnameResponse>();

    return (new GetAccountResponse {
      AccountId = accountId,
      Description = cache.Description,
      Contacts = cache.Contacts,
      Hostnames = hostnames.ToArray()
    }, IDomainResult.Success());
  }

  public void Dispose() {
    _lockManager?.Dispose();
  }

  #endregion
}
