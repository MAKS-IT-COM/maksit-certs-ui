using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Models.CertsUI.Account.Requests;
using MaksIT.CertsUI.Models.CertsUI.Account.Responses;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Services;

public interface IAccountService {
  Task<Result<GetAccountResponse[]?>> GetAccountsAsync(CertsUIAuthorizationData certsAuthorizationData);
  Task<Result<GetAccountResponse?>> GetAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId);
  Task<Result<GetAccountResponse?>> PostAccountAsync(CertsUIAuthorizationData certsAuthorizationData, PostAccountRequest requestData);
  Task<Result<GetAccountResponse?>> PatchAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId, PatchAccountRequest requestData);
  Task<Result> DeleteAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId);
}

public class AccountService(
  ILogger<AccountService> logger,
  IOptions<Configuration> appSettings,
  ICacheService cacheService,
  ICertsFlowService certsFlowService,
  AccountToResponseMapper accountToResponseMapper
) : ServiceBase(
  logger,
  appSettings
), IAccountService {

  private readonly ICacheService _cacheService = cacheService;
  private readonly ICertsFlowService _certsFlowService = certsFlowService;
  private readonly AccountToResponseMapper _accountToResponseMapper = accountToResponseMapper;

  #region Accounts

  public async Task<Result<GetAccountResponse[]?>> GetAccountsAsync(CertsUIAuthorizationData certsAuthorizationData) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<GetAccountResponse[]?>(null);

    var accountsFromCacheResult = await _cacheService.LoadAccountsFromCacheAsync();
    if (!accountsFromCacheResult.IsSuccess || accountsFromCacheResult.Value == null) {
      return accountsFromCacheResult
        .ToResultOfType<GetAccountResponse[]?>(_ => null);
    }

    var accounts = accountsFromCacheResult.Value
      .Select(x => _accountToResponseMapper.MapToResponse(x.AccountId, x))
      .ToArray();

    return Result<GetAccountResponse[]?>.Ok(accounts);
  }

  public async Task<Result<GetAccountResponse?>> GetAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<GetAccountResponse?>(null);

    var loadFromCacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!loadFromCacheResult.IsSuccess || loadFromCacheResult.Value == null) {
      return loadFromCacheResult.ToResultOfType<GetAccountResponse?>(_ => null);
    }

    var cache = loadFromCacheResult.Value;

    return Result<GetAccountResponse?>.Ok(_accountToResponseMapper.MapToResponse(accountId, cache));
  }

  public async Task<Result<GetAccountResponse?>> PostAccountAsync(CertsUIAuthorizationData certsAuthorizationData, PostAccountRequest requestData) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<GetAccountResponse?>(null);

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

    return Result<GetAccountResponse?>.Ok(_accountToResponseMapper.MapToResponse(accountId, cache));
  }

  public async Task<Result<GetAccountResponse?>> PatchAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId, PatchAccountRequest requestData) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<GetAccountResponse?>(null);

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

    return Result<GetAccountResponse?>.Ok(_accountToResponseMapper.MapToResponse(accountId, loadAccountResult.Value));
  }

  public async Task<Result> DeleteAccountAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac;

    // TODO: Revoke all certificates

    // Remove from cache
    return await _cacheService.DeleteAccountCacheAsync(accountId);
  }
  #endregion

}
