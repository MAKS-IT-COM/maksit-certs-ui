using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistance.Services;

/// <summary>
/// API key persistence (hash-at-rest, no scopes). Async surface; naming follows the same <c>Persistance</c> / <c>Linq2Db</c> conventions as other MaksIT engines.
/// </summary>
public interface IAPIKeyPersistanceService {

  #region Read

  /// <summary>Legacy opaque keys: lookup by SHA-256 hex of UTF-8 secret; only rows with empty <c>KeySalt</c>.</summary>
  Task<Result<Guid>> TryValidateLegacyKeyHashAsync(string keyHashHex, CancellationToken cancellationToken = default);

  Task<Result<ApiKey?>> ReadByIdAsync(Guid id, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  Task<Result> InsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

  Task<Result> UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

  Task<Result> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

  #endregion
}
