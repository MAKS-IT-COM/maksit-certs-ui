using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Registration cache use cases (load/save, zip import/export). Orchestrates <see cref="Persistance.Services.IRegistrationCachePersistanceService"/> only from the engine layer.
/// </summary>
public interface IRegistrationCacheDomainService {

  #region Read

  Task<Result<RegistrationCache[]?>> LoadAllAsync(CancellationToken cancellationToken = default);

  Task<Result<RegistrationCache?>> LoadAsync(Guid accountId, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  Task<Result> SaveAsync(Guid accountId, RegistrationCache cache, CancellationToken cancellationToken = default);

  Task<Result> DeleteAllAsync(CancellationToken cancellationToken = default);

  Task<Result> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default);

  #endregion

  #region Zip import/export

  Task<Result<byte[]>> DownloadCacheZipAsync(CancellationToken cancellationToken = default);

  Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId, CancellationToken cancellationToken = default);

  Task<Result> UploadCacheZipAsync(byte[] zipBytes, CancellationToken cancellationToken = default);

  Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes, CancellationToken cancellationToken = default);

  #endregion
}
