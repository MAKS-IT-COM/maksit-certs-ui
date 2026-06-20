using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;

namespace MaksIT.CertsUI.Services;

public interface ICacheService {

  #region Read
  Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync();
  Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId);
  #endregion

  #region Write
  Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<Result> DeleteCacheAsync(CertsUIAuthorizationData certsAuthorizationData);
  Task<Result> DeleteAccountCacheAsync(Guid accountId);
  #endregion

  #region Zip import/export
  Task<Result<byte[]?>> DownloadCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData);
  Task<Result<byte[]?>> DownloadAccountCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId);
  Task<Result> UploadCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, byte[] zipBytes);
  Task<Result> UploadAccountCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId, byte[] zipBytes);
  #endregion
}

/// <summary>Web API façade for registration cache operations; delegates to <see cref="IRegistrationCacheDomainService"/>.</summary>
public class CacheService(
  ILogger<CacheService> logger,
  IOptions<Configuration> appSettings,
  IRegistrationCacheDomainService registrationCacheDomain
) : ServiceBase(
  logger,
  appSettings
), ICacheService {

  #region Read

  public Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() =>
    registrationCacheDomain.LoadAllAsync();

  public Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) =>
    registrationCacheDomain.LoadAsync(accountId);

  #endregion

  #region Write

  public Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) =>
    registrationCacheDomain.SaveAsync(accountId, cache);

  public Task<Result> DeleteCacheAsync(CertsUIAuthorizationData certsAuthorizationData) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return Task.FromResult(rbac);
    return registrationCacheDomain.DeleteAllAsync();
  }

  public Task<Result> DeleteAccountCacheAsync(Guid accountId) =>
    registrationCacheDomain.DeleteAsync(accountId);

  #endregion

  #region Zip import/export

  public Task<Result<byte[]?>> DownloadCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return Task.FromResult(rbac.ToResultOfType<byte[]?>(null));
    return registrationCacheDomain.DownloadCacheZipAsync();
  }

  public Task<Result<byte[]?>> DownloadAccountCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return Task.FromResult(rbac.ToResultOfType<byte[]?>(null));
    return registrationCacheDomain.DownloadAccountCacheZipAsync(accountId);
  }

  public Task<Result> UploadCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, byte[] zipBytes) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return Task.FromResult(rbac);
    return registrationCacheDomain.UploadCacheZipAsync(zipBytes);
  }

  public Task<Result> UploadAccountCacheZipAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId, byte[] zipBytes) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return Task.FromResult(rbac);
    return registrationCacheDomain.UploadAccountCacheZipAsync(accountId, zipBytes);
  }

  #endregion
}
