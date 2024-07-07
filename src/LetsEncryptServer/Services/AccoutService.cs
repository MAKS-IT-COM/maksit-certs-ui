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
  Task<(GetAccountResponse?, IDomainResult)> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData);
  Task<IDomainResult> DeleteAccountAsync(Guid accountId); 
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

    var (accountId, newCertsResult) = await _certsFlowService.FullFlow(
          requestData.IsStaging,
          null,
          requestData.Description,
          requestData.Contacts,
          requestData.ChallengeType,
          requestData.Hostnames
        );

    if (!newCertsResult.IsSuccess || accountId == null)
      return (null, newCertsResult);



    var (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId.Value);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId.Value, cache));
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

    if (requestData.IsDisabled != null) {
      switch (requestData.IsDisabled.Op) { 
        case PatchOperation.Replace:
          cache.IsDisabled = requestData.IsDisabled.Value;
          break;
      }
    }

    if (requestData.Contacts?.Any() == true) {
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

    var hostnamesToAdd = new List<string>();
    var hostnamesToRemove = new List<string>();

    if (requestData.Hostnames?.Any() == true) {
      var hostnames = cache.GetHosts().ToList();
      foreach (var action in requestData.Hostnames) {

        if (action.Hostname != null) {
          switch (action.Hostname.Op) {
            case PatchOperation.Add:
              hostnamesToAdd.Add(action.Hostname.Value);

              break;

            case PatchOperation.Replace:
              if (action.Hostname.Index != null && action.Hostname.Index >= 0 && action.Hostname.Index < hostnames.Count)
                hostnames[action.Hostname.Index.Value].Hostname = action.Hostname.Value;
              break;

            case PatchOperation.Remove:
              hostnamesToRemove.Add(action.Hostname.Value);

            
              break;
          }
        }

        if (action.IsDisabled != null) {
          switch (action.IsDisabled.Op) {
            case PatchOperation.Replace:
              
              break;
          }
        }
      }
    }

    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return (null, saveResult);
    }

    if (hostnamesToAdd.Count > 0) {
      var (_, newCertsResult) = await _certsFlowService.FullFlow(
        cache.IsStaging,
        cache.AccountId,
        cache.Description,
        cache.Contacts,
        cache.ChallengeType,
        hostnamesToAdd.ToArray()
      );

      if (!newCertsResult.IsSuccess)
        return (null, newCertsResult);
    }

    if (hostnamesToRemove.Count > 0) {
      var revokeResult = await _certsFlowService.FullRevocationFlow(
        cache.IsStaging,
        cache.AccountId,
        cache.Description,
        cache.Contacts,
        hostnamesToRemove.ToArray()
      );

      if (!revokeResult.IsSuccess)
        return (null, revokeResult);
    }

    (cache, loadResult) = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadResult.IsSuccess || cache == null) {
      return (null, loadResult);
    }

    return IDomainResult.Success(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<IDomainResult> DeleteAccountAsync(Guid accountId) {
    // TODO: Revoke all certificates

    // Remove from cache
    return await _cacheService.DeleteFromCacheAsync(accountId);
  }
  #endregion

  #region Helper Methods

  private List<GetHostnameResponse> GetHostnamesFromCache(RegistrationCache cache) {
    var hosts = cache.GetHosts().Select(x => new GetHostnameResponse {
      Hostname = x.Hostname,
      Expires = x.Expires,
      IsUpcomingExpire = x.IsUpcomingExpire,
      IsDisabled = x.IsDisabled
    }).ToList();

    return hosts;
  }

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
