using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using DomainResults.Common;
using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;
using MaksIT.Models.LetsEncryptServer.Cache.Responses;
using Models.LetsEncryptServer.Cache.Responses;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<(RegistrationCache?, IDomainResult)> LoadFromCacheAsync(Guid accountId);
  Task<IDomainResult> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<IDomainResult> DeleteFromCacheAsync(Guid accountId);
}

public interface ICacheRestService {
  Task<(GetAccountsResponse?, IDomainResult)> GetAccountsAsync();
  Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId);

  #region Contacts
  Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId);
  Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactRequest requestData);
  Task<IDomainResult> DeleteContactAsync(Guid accountId, int index);
  #endregion

  #region Hostnames
  Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId);
  #endregion
}

public class CacheService : ICacheService, ICacheRestService, IDisposable {

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



  #region RestService
  public async Task<(GetAccountsResponse?, IDomainResult)> GetAccountsAsync() {
    await _cacheLock.WaitAsync();

    try {
      var cacheFiles = Directory.GetFiles(_cacheDirectory);
      if (cacheFiles == null)
        return IDomainResult.Success(new GetAccountsResponse {
          Accounts = Array.Empty<GetAccountResponse>()
        });

      var accountIds = cacheFiles.Select(x => Path.GetFileNameWithoutExtension(x).ToGuid());

      var accounts = new List<GetAccountResponse>();
      foreach (var accountId in accountIds) {
        var (account, getAccountResult) = await GetAccountAsync(accountId);
        if(!getAccountResult.IsSuccess || account == null)
          return (null, getAccountResult);

        accounts.Add(account);
      }
       
      return IDomainResult.Success(new GetAccountsResponse {
        Accounts = accounts.ToArray()
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

  public async Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId) {

    await _cacheLock.WaitAsync();

    try {
        var (registrationCache, gerRegistrationCacheResult) = await LoadFromCacheAsync(accountId);
        if (!gerRegistrationCacheResult.IsSuccess || registrationCache == null)
          return (null, gerRegistrationCacheResult);

      return IDomainResult.Success(new GetAccountResponse {
        AccountId = accountId,
        Description = registrationCache.Description,
        Contacts = registrationCache.Contacts,
        Hostnames = registrationCache.GetHostsWithUpcomingSslExpiry()
      });
    }
    catch (Exception ex) {
      var message = "Error listing cache files";
      _logger.LogError(ex, message);

      return IDomainResult.Failed<GetAccountResponse?>(message);
    }
    finally {
      _cacheLock.Release();
    }
  }


  #region Contacts
  /// <summary>
  /// Retrieves the contacts list for the account.
  /// </summary>
  /// <param name="accountId">The ID of the account.</param>
  /// <returns>The contacts list and domain result.</returns>
  public async Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return (null, loadResult);

    return IDomainResult.Success(new GetContactsResponse {
      Contacts = cache.Contacts ?? Array.Empty<string>()
    });
  }

  /// <summary>
  /// Adds new contacts to the account. This method initializes the contacts list if it is null.
  /// </summary>
  /// <param name="accountId">The ID of the account.</param>
  /// <param name="requestData">The request containing the contacts to add.</param>
  /// <returns>The updated account response and domain result.</returns>
  public async Task<(GetAccountResponse?, IDomainResult)> PostContactAsync(Guid accountId, PostContactsRequest requestData) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return (null, loadResult);

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    if (requestData.Contacts != null) {
      contacts.AddRange(requestData.Contacts);
    }

    cache.Contacts = contacts.ToArray();
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess)
      return (null, saveResult);

    return (new GetAccountResponse {
      AccountId = accountId,
      Description = cache.Description,
      Contacts = cache.Contacts,
      Hostnames = cache.GetHostsWithUpcomingSslExpiry()
    }, IDomainResult.Success());
  }

  /// <summary>
  /// Replaces the entire contacts list for the account.
  /// </summary>
  /// <param name="accountId">The ID of the account.</param>
  /// <param name="requestData">The request containing the new contacts list.</param>
  /// <returns>The updated account response and domain result.</returns>
  public async Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return (null, loadResult);

    cache.Contacts = requestData.Contacts;
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess)
      return (null, saveResult);

    return (new GetAccountResponse {
      AccountId = accountId,
      Description = cache.Description,
      Contacts = cache.Contacts,
      Hostnames = cache.GetHostsWithUpcomingSslExpiry()
    }, IDomainResult.Success());
  }

  /// <summary>
  /// Partially updates the contacts list for the account. Supports add, replace, and remove operations.
  /// </summary>
  /// <param name="accountId">The ID of the account.</param>
  /// <param name="requestData">The request containing the patch operations for contacts.</param>
  /// <returns>The updated account response and domain result.</returns>
  public async Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactRequest requestData) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return (null, loadResult);

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    foreach (var contact in requestData.Contacts) {
      switch (contact.Op) {
        case PatchOperation.Add:
          if (contact.Value != null)
            contacts.Add(contact.Value);
          break;
        case PatchOperation.Replace:
          if (contact.Index.HasValue && contact.Index.Value >= 0 && contact.Index.Value < contacts.Count && contact.Value != null)
            contacts[contact.Index.Value] = contact.Value;
          break;
        case PatchOperation.Remove:
          if (contact.Index.HasValue && contact.Index.Value >= 0 && contact.Index.Value < contacts.Count)
            contacts.RemoveAt(contact.Index.Value);
          break;
        default:
          return (null, IDomainResult.Failed("Invalid patch operation."));
      }
    }

    cache.Contacts = contacts.ToArray();
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess)
      return (null, saveResult);

    return (new GetAccountResponse {
      AccountId = accountId,
      Description = cache.Description,
      Contacts = cache.Contacts,
      Hostnames = cache.GetHostsWithUpcomingSslExpiry()
    }, IDomainResult.Success());
  }

  /// <summary>
  /// Deletes a contact from the account by index.
  /// </summary>
  /// <param name="accountId">The ID of the account.</param>
  /// <param name="index">The index of the contact to remove.</param>
  /// <returns>The domain result indicating success or failure.</returns>
  public async Task<IDomainResult> DeleteContactAsync(Guid accountId, int index) {
    var (cache, loadResult) = await LoadFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null)
      return loadResult;

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    if (index >= 0 && index < contacts.Count)
      contacts.RemoveAt(index);

    cache.Contacts = contacts.ToArray();
    var saveResult = await SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess)
      return saveResult;

    return IDomainResult.Success();
  }

  #endregion

  #region Hostnames
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
  #endregion

  #endregion


  public void Dispose() {
    _cacheLock?.Dispose();
  }
}
