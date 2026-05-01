using LinqToDB;
using Microsoft.Extensions.Logging;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Mappers;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IAcmeSessionPersistanceService"/> for PostgreSQL.
/// </summary>
public sealed class AcmeSessionPersistanceServiceLinq2Db(
  ILogger<AcmeSessionPersistanceServiceLinq2Db> logger,
  ICertsDataConnectionFactory connectionFactory
) : IAcmeSessionPersistanceService {

  private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

  private readonly ILogger<AcmeSessionPersistanceServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

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

      using var db = _connectionFactory.Create();
      var existing = db.GetTable<AcmeSessionDto>()
        .Where(x => x.SessionId == sessionId)
        .FirstOrDefault();

      if (existing == null) {
        db.Insert(new AcmeSessionDto {
          SessionId = sessionId,
          PayloadJson = json,
          UpdatedAtUtc = now,
          ExpiresAtUtc = expires
        });
      }
      else {
        existing.PayloadJson = json;
        existing.UpdatedAtUtc = now;
        existing.ExpiresAtUtc = expires;
        db.Update(existing);
      }

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error saving ACME session {SessionId}", sessionId);
      return Task.FromResult(Result.InternalServerError(["An error occurred while saving the ACME session.", .. ex.ExtractMessages()]));
    }
  }
}
