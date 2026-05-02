using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Mappers;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IAcmeSessionPersistenceService"/> for PostgreSQL.
/// </summary>
public sealed class AcmeSessionPersistenceServiceLinq2Db(
  ILogger<AcmeSessionPersistenceServiceLinq2Db> logger,
  ICertsUIDataConnectionFactory connectionFactory
) : IAcmeSessionPersistenceService {

  private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

  private readonly ILogger<AcmeSessionPersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Task<Result<State?>> LoadAsync(Guid sessionId, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var now = DateTimeOffset.UtcNow;
      var row = db.GetTable<AcmeSessionDto>()
        .Where(x => x.SessionId == sessionId && x.ExpiresAtUtc > now)
        .FirstOrDefault();
      if (row == null)
        return Task.FromResult(Result<State?>.Ok(null));

      try {
        var state = AcmeSessionPayloadMapper.FromPayloadJson(row.PayloadJson);
        return Task.FromResult(Result<State?>.Ok(state));
      }
      catch (Exception ex) {
        _logger.LogWarning(ex, "Failed to deserialize ACME session {SessionId}; returning empty.", sessionId);
        return Task.FromResult(Result<State?>.Ok(null));
      }
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error loading ACME session {SessionId}", sessionId);
      return Task.FromResult(Result<State?>.InternalServerError(null, ["An error occurred while loading the ACME session.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> SaveAsync(Guid sessionId, State state, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(state);

    try {
      var json = AcmeSessionPayloadMapper.ToPayloadJson(state);
      var now = DateTimeOffset.UtcNow;
      var expires = now.Add(SessionTtl);
      var accountScope = state.Cache?.AccountId;

      using var db = _connectionFactory.Create();
      var existing = db.GetTable<AcmeSessionDto>()
        .Where(x => x.SessionId == sessionId)
        .FirstOrDefault();

      if (existing == null) {
        db.Insert(new AcmeSessionDto {
          SessionId = sessionId,
          AccountScopeId = accountScope,
          PayloadJson = json,
          UpdatedAtUtc = now,
          ExpiresAtUtc = expires
        });
      }
      else {
        existing.PayloadJson = json;
        existing.UpdatedAtUtc = now;
        existing.ExpiresAtUtc = expires;
        existing.AccountScopeId = accountScope;
        db.Update(existing);
      }

      if (accountScope.HasValue) {
        var removed = DeleteOtherSessionsForSameAccount(db, accountScope.Value, sessionId);
        if (removed > 0 && _logger.IsEnabled(LogLevel.Debug))
          _logger.LogDebug("Removed {Count} other ACME session row(s) for account scope {AccountId}.", removed, accountScope.Value);
      }

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error saving ACME session {SessionId}", sessionId);
      return Task.FromResult(Result.InternalServerError(["An error occurred while saving the ACME session.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<int>> DeleteExpiredAsync(CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var deleted = DeleteExpiredRows(db, DateTimeOffset.UtcNow);
      return Task.FromResult(Result<int>.Ok(deleted));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting expired ACME sessions.");
      return Task.FromResult(Result<int>.InternalServerError(0, ["Failed to delete expired ACME sessions.", .. ex.ExtractMessages()]));
    }
  }

  private static int DeleteExpiredRows(DataConnection db, DateTimeOffset utcNow) =>
    db.GetTable<AcmeSessionDto>().Where(x => x.ExpiresAtUtc <= utcNow).Delete();

  /// <summary>Other browser sessions for the same registration account (<see cref="Domain.Certs.RegistrationCache.AccountId"/>)—not the current <paramref name="keepSessionId"/>.</summary>
  private static int DeleteOtherSessionsForSameAccount(DataConnection db, Guid accountScopeId, Guid keepSessionId) =>
    db.GetTable<AcmeSessionDto>()
      .Where(x => x.AccountScopeId == accountScopeId && x.SessionId != keepSessionId)
      .Delete();
}
