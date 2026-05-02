using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

/// <summary>
/// PostgreSQL <c>acme_sessions</c>: load/save JSON payload for per-session ACME <see cref="State"/>.
/// </summary>
public interface IAcmeSessionPersistenceService {

  #region Read

  /// <summary>
  /// Loads a non-expired session row. Returns <see cref="Result{T}.IsSuccess"/> with <c>null</c> when none match.
  /// </summary>
  Task<Result<State?>> LoadAsync(Guid sessionId, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  /// <summary>
  /// Upserts session payload and refreshes <c>updated_at_utc</c> / <c>expires_at_utc</c>.
  /// When <paramref name="state"/> carries <c>RegistrationCache</c>, persists <c>account_scope_id</c> and removes other browser sessions for the same registration account (same idea as pruning old JWTs on the <c>User</c> aggregate). Pre-account configure phase leaves scope null—no sibling delete. Optional periodic <see cref="DeleteExpiredAsync"/> remains for TTL hygiene.
  /// </summary>
  Task<Result> SaveAsync(Guid sessionId, State state, CancellationToken cancellationToken = default);

  #endregion

  #region Maintenance

  /// <summary>
  /// Deletes rows with <c>expires_at_utc</c> at or before UTC now.
  /// </summary>
  Task<Result<int>> DeleteExpiredAsync(CancellationToken cancellationToken = default);

  #endregion
}
