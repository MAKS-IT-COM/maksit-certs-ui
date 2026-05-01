using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistance.Services;

/// <summary>
/// PostgreSQL <c>acme_sessions</c>: load/save JSON payload for per-session ACME <see cref="State"/>.
/// </summary>
public interface IAcmeSessionPersistanceService {

  #region Read

  /// <summary>
  /// Loads a non-expired session row. Returns <see cref="Result{T}.IsSuccess"/> with <c>null</c> when none match.
  /// </summary>
  Task<Result<State?>> LoadAsync(Guid sessionId, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  /// <summary>
  /// Upserts session payload and refreshes <c>updated_at_utc</c> / <c>expires_at_utc</c>.
  /// </summary>
  Task<Result> SaveAsync(Guid sessionId, State state, CancellationToken cancellationToken = default);

  #endregion
}
