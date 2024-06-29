using DomainResults.Common;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models;
using MaksIT.Models.LetsEncryptServer.Account.Requests;
using MaksIT.Models.LetsEncryptServer.Account.Responses;

namespace MaksIT.LetsEncryptServer.Services;


public interface IAccountInternalService {

}


public interface IAccountRestService {
  Task<(GetAccountResponse[]?, IDomainResult)> GetAccountsAsync();
  Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId);
  Task<(GetAccountResponse?, IDomainResult)> PostAccountAsync(PostAccountRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PutAccountAsync(Guid accountId, PutAccountRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData);
  Task<IDomainResult> DeleteAccountAsync(Guid accountId);
  Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId);
  Task<(GetContactsResponse?, IDomainResult)> PostContactsAsync(Guid accountId, PostContactsRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData);
  Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactsRequest requestData);
  Task<IDomainResult> DeleteContactAsync(Guid accountId, int index);
  Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId);
}

public interface IAccountService : IAccountInternalService, IAccountRestService { }

public class AccountService : IAccountService {

  private readonly ILogger<CacheService> _logger;
  private readonly ICacheService _cacheService;
  private readonly ICertsInternalService _certsFlowService;

  public AccountService(
    ILogger<CacheService> logger,
    ICacheService cacheService,
    ICertsFlowService certsFlowService
  ) {
    _logger = logger;
    _cacheService = cacheService;
    _certsFlowService = certsFlowService;
  }

  #region Accounts

  public async Task<(GetAccountResponse[]?, IDomainResult)> GetAccountsAsync() {

    var (caches, result) = await _cacheService.LoadAccountsFromCacheAsync();
    if (!result.IsSuccess || caches == null) {
      return (null, result);
    }

    var accounts = caches
      .Select(x => CreateGetAccountResponse(x.AccountId, x))
      .ToArray();

    return IDomainResult.Success(accounts);
  }

  public async Task<(GetAccountResponse?, IDomainResult)> GetAccountAsync(Guid accountId) {
    var (cache, result) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!result.IsSuccess || cache == null) {
      return (null, result);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PostAccountAsync(PostAccountRequest requestData) {

    // TODO: check for overlapping hostnames in already existing accounts

    var (sessionId, configureClientResult) = await _certsFlowService.ConfigureClientAsync(requestData.IsStaging);
    if (!configureClientResult.IsSuccess || sessionId == null) {
      //LogErrors(configureClientResult.Errors);
      return (null, configureClientResult);
    }
    var sessionIdValue = sessionId.Value;

    var (_, initResult) = await _certsFlowService.InitAsync(sessionIdValue, null, requestData.Description, requestData.Contacts);
    if (!initResult.IsSuccess) {
      //LogErrors(initResult.Errors);
      return (null, initResult);
    }

    var (_, newOrderResult) = await _certsFlowService.NewOrderAsync(sessionIdValue, requestData.Hostnames, requestData.ChallengeType);
    if (!newOrderResult.IsSuccess) {
      //LogErrors(newOrderResult.Errors);
      return (null, newOrderResult);
    }

    var challengeResult = await _certsFlowService.CompleteChallengesAsync(sessionIdValue);
    if (!challengeResult.IsSuccess) {
      //LogErrors(challengeResult.Errors);
      return (null, challengeResult);
    }

    var getOrderResult = await _certsFlowService.GetOrderAsync(sessionIdValue, requestData.Hostnames);
    if (!getOrderResult.IsSuccess) {
      //LogErrors(getOrderResult.Errors);
      return (null, getOrderResult);
    }

    var certs = await _certsFlowService.GetCertificatesAsync(sessionIdValue, requestData.Hostnames);
    if (!certs.IsSuccess) {
      //LogErrors(certs.Errors);
      return (null, certs);
    }

    var (_, applyCertsResult) = await _certsFlowService.ApplyCertificatesAsync(sessionIdValue, requestData.Hostnames);
    if (!applyCertsResult.IsSuccess) {
      //LogErrors(applyCertsResult.Errors);
      return (null, applyCertsResult);
    }

    return IDomainResult.Success<GetAccountResponse?>(null);
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PutAccountAsync(Guid accountId, PutAccountRequest requestData) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    cache.Description = requestData.Description;
    cache.Contacts = requestData.Contacts;

    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
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

    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<IDomainResult> DeleteAccountAsync(Guid accountId) {
    return await _cacheService.DeleteFromCacheAsync(accountId);
  }
  #endregion

  #region Contacts Operations

  public async Task<(GetContactsResponse?, IDomainResult)> GetContactsAsync(Guid accountId) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    return IDomainResult.Success(new GetContactsResponse {
      Contacts = cache.Contacts ?? Array.Empty<string>()
    });
  }

  public async Task<(GetContactsResponse?, IDomainResult)> PostContactsAsync(Guid accountId, PostContactsRequest requestData) {
    return IDomainResult.Failed<GetContactsResponse?>("Not implemented");
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PutContactsAsync(Guid accountId, PutContactsRequest requestData) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    cache.Contacts = requestData.Contacts;
    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<(GetAccountResponse?, IDomainResult)> PatchContactsAsync(Guid accountId, PatchContactsRequest requestData) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
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
    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<IDomainResult> DeleteContactAsync(Guid accountId, int index) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return loadResult;
    }

    var contacts = cache.Contacts?.ToList() ?? new List<string>();

    if (index >= 0 && index < contacts.Count) {
      contacts.RemoveAt(index);
    }

    cache.Contacts = contacts.ToArray();
    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return saveResult;
    }

    return IDomainResult.Success();
  }

  #endregion

  #region Hostnames Operations

  public async Task<(GetHostnamesResponse?, IDomainResult)> GetHostnames(Guid accountId) {
    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
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
      IsUpcomingExpire = x.IsUpcomingExpire,
      IsDisabled = x.IsDisabled
    }).ToList();

    return hosts;
  }

  #endregion

  #region Helper Methods

  private GetAccountResponse CreateGetAccountResponse(Guid accountId, RegistrationCache cache) {
    var hostnames = GetHostnamesFromCache(cache) ?? [];

    return new GetAccountResponse {
      AccountId = accountId,
      IsDisabled = cache.IsDisabled,
      Description = cache.Description,
      Contacts = cache.Contacts,
      ChallengeType = cache.ChallengeType,
      Hostnames = [.. hostnames],
      IsStaging = cache.IsStaging
    };
  }


  #endregion
}
