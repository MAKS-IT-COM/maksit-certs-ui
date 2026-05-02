using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

/// <summary>
/// Persists and loads <see cref="RegistrationCache"/> for ACME accounts (PostgreSQL JSON payload).
/// </summary>
public interface IRegistrationCachePersistenceService {

  #region Read

  Task<Result<RegistrationCache[]?>> LoadAllAsync(CancellationToken cancellationToken = default);

  Task<Result<RegistrationCache?>> LoadAsync(Guid accountId, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  Task<Result> SaveAsync(Guid accountId, RegistrationCache cache, CancellationToken cancellationToken = default);

  #endregion

  #region Delete

  Task<Result> DeleteAllAsync(CancellationToken cancellationToken = default);

  Task<Result> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default);

  #endregion
}
