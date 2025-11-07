
using LetsEncryptServer.Abstractions;
using MaksIT.Core.Webapi.Models;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models;
using MaksIT.Models.LetsEncryptServer.Account.Requests;
using MaksIT.Models.LetsEncryptServer.Account.Responses;
using MaksIT.Results;
using System;
using static System.Collections.Specialized.BitVector32;

namespace MaksIT.LetsEncryptServer.Services;


public interface IAccountInternalService {

}


public interface IAccountRestService {
  Task<Result<GetAccountResponse[]?>> GetAccountsAsync();
  Task<Result<GetAccountResponse?>> GetAccountAsync(Guid accountId);
  Task<Result<GetAccountResponse?>> PostAccountAsync(PostAccountRequest requestData);
  Task<Result<GetAccountResponse?>> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData);
  Task<Result> DeleteAccountAsync(Guid accountId);
}

public interface IAccountService : IAccountInternalService, IAccountRestService { }

public class AccountService : ServiceBase, IAccountService {

  private readonly ILogger<CacheService> _logger;
  private readonly ICacheService _cacheService;
  private readonly ICertsFlowService _certsFlowService;

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

  public async Task<Result<GetAccountResponse[]?>> GetAccountsAsync() {

    var accountsFromCacheResult = await _cacheService.LoadAccountsFromCacheAsync();
    if (!accountsFromCacheResult.IsSuccess || accountsFromCacheResult.Value == null) {
      return accountsFromCacheResult
        .ToResultOfType<GetAccountResponse[]?>(_ => null);
    }

    var accounts = accountsFromCacheResult.Value
      .Select(x => CreateGetAccountResponse(x.AccountId, x))
      .ToArray();

    return Result<GetAccountResponse[]?>.Ok(accounts);
  }

  public async Task<Result<GetAccountResponse?>> GetAccountAsync(Guid accountId) {
    var loadFromCacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadFromCacheResult.IsSuccess || loadFromCacheResult.Value == null) {
      return loadFromCacheResult.ToResultOfType<GetAccountResponse?>(_ => null);
    }

    var cache = loadFromCacheResult.Value;

    return Result<GetAccountResponse?>.Ok(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<Result<GetAccountResponse?>> PostAccountAsync(PostAccountRequest requestData) {

    var fullFlowResult = await _certsFlowService.FullFlow(
          requestData.IsStaging,
          null,
          requestData.Description,
          requestData.Contacts,
          requestData.ChallengeType,
          requestData.Hostnames
        );

    if (!fullFlowResult.IsSuccess || fullFlowResult.Value == null)
      return fullFlowResult.ToResultOfType<GetAccountResponse?>(_ => null);

    var accountId = fullFlowResult.Value.Value;

    var loadAccountFromCacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadAccountFromCacheResult.IsSuccess || loadAccountFromCacheResult.Value == null) {
      return loadAccountFromCacheResult.ToResultOfType<GetAccountResponse?>(_ => null);
    }

    var cache = loadAccountFromCacheResult.Value;

    return Result<GetAccountResponse?>.Ok(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<Result<GetAccountResponse?>> PatchAccountAsync(Guid accountId, PatchAccountRequest requestData) {
    var loadAccountResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadAccountResult.IsSuccess || loadAccountResult.Value == null) {
      return loadAccountResult.ToResultOfType<GetAccountResponse?>(_ => null);
    }

    var cache = loadAccountResult.Value;

    if (requestData.TryGetOperation(nameof(requestData.Description), out var patchOperation)) {
      switch (patchOperation) {
        case PatchOperation.SetField:
          if (requestData.Description == null)
            return PatchFieldIsNotDefined<GetAccountResponse?>(nameof(requestData.Description));

            cache.Description = requestData.Description;
          break;
        default:
          return UnsupportedPatchOperationResponse<GetAccountResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.IsDisabled), out patchOperation)) {
      switch (patchOperation) {
        case PatchOperation.SetField:
          if (requestData.IsDisabled == null)
            return PatchFieldIsNotDefined<GetAccountResponse?>(nameof(requestData.IsDisabled));

          cache.IsDisabled = requestData.IsDisabled.Value;
          break;
        default:
          return UnsupportedPatchOperationResponse<GetAccountResponse?>();
      }
    }

    if (requestData.TryGetOperation(nameof(requestData.Contacts), out patchOperation)) {
      switch (patchOperation) {
        case PatchOperation.SetField:
          if (requestData.Contacts == null)
            return PatchFieldIsNotDefined<GetAccountResponse?>(nameof(requestData.Contacts));
          cache.Contacts = requestData.Contacts.ToArray();
          break;
      }
    }

    #region Patch Hostnames
    var hostnamesToAdd = new List<string>();
    var hostnamesToRemove = new List<string>();

    foreach (var hostnameRequestData in requestData.Hostnames ?? []) {
      if (hostnameRequestData.TryGetOperation("collectionItemOperation", out patchOperation)) {

        if (hostnameRequestData.Hostname == null)
          return PatchFieldIsNotDefined<GetAccountResponse?>(nameof(hostnameRequestData.Hostname));

        switch (patchOperation) {
          case PatchOperation.AddToCollection:
            hostnamesToAdd.Add(hostnameRequestData.Hostname);
            break;

          case PatchOperation.RemoveFromCollection:
            hostnamesToRemove.Add(hostnameRequestData.Hostname);
            break;
        }
      }
    }

    var saveResult = await _cacheService.SaveToCacheAsync(accountId, cache);
    if (!saveResult.IsSuccess) {
      return saveResult.ToResultOfType<GetAccountResponse?>(default);
    }

    if (hostnamesToAdd.Count > 0) {
      var fullFlowResult = await _certsFlowService.FullFlow(
        cache.IsStaging,
        cache.AccountId,
        cache.Description,
        cache.Contacts,
        cache.ChallengeType,
        hostnamesToAdd.ToArray()
      );

      if (!fullFlowResult.IsSuccess)
        return fullFlowResult.ToResultOfType<GetAccountResponse?>(_ => null);
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
        return revokeResult.ToResultOfType<GetAccountResponse?>(default);
    }
    #endregion

    loadAccountResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadAccountResult.IsSuccess || loadAccountResult.Value == null) {
      return loadAccountResult.ToResultOfType<GetAccountResponse?>(_ => null);
    }

    return Result<GetAccountResponse?>.Ok(CreateGetAccountResponse(accountId, cache));
  }

  public async Task<Result> DeleteAccountAsync(Guid accountId) {
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
