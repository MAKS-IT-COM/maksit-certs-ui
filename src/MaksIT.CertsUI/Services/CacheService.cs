using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;

namespace MaksIT.CertsUI.Services;

public interface ICacheService {
  Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync();
  Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId);
  Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<Result<byte[]>> DownloadCacheZipAsync();
  Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId);
  Task<Result> UploadCacheZipAsync(byte[] zipBytes);
  Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes);
  Task<Result> DeleteCacheAsync();
  Task<Result> DeleteAccountCacheAsync(Guid accountId);
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

  public Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() =>
    registrationCacheDomain.LoadAllAsync();

  public Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) =>
    registrationCacheDomain.LoadAsync(accountId);

  public Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) =>
    registrationCacheDomain.SaveAsync(accountId, cache);

  public Task<Result<byte[]>> DownloadCacheZipAsync() =>
    registrationCacheDomain.DownloadCacheZipAsync();

  public Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId) =>
    registrationCacheDomain.DownloadAccountCacheZipAsync(accountId);

  public Task<Result> UploadCacheZipAsync(byte[] zipBytes) =>
    registrationCacheDomain.UploadCacheZipAsync(zipBytes);

  public Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes) =>
    registrationCacheDomain.UploadAccountCacheZipAsync(accountId, zipBytes);

  public Task<Result> DeleteCacheAsync() =>
    registrationCacheDomain.DeleteAllAsync();

  public Task<Result> DeleteAccountCacheAsync(Guid accountId) =>
    registrationCacheDomain.DeleteAsync(accountId);
}
