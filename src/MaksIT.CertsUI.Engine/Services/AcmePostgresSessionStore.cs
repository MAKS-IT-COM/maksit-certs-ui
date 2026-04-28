using LinqToDB;
using LinqToDB.Data;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.Data;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.Services;

/// <summary>PostgreSQL-backed ACME session state (replaces in-process <c>IMemoryCache</c>).</summary>
public sealed class AcmePostgresSessionStore(
  ICertsEngineConfiguration config,
  ILogger<AcmePostgresSessionStore> logger
) : IAcmeSessionStore {

  private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

  private DataConnection CreateConnection() {
    var options = new DataOptions()
      .UseConnectionString(ProviderName.PostgreSQL, config.ConnectionString)
      .UseMappingSchema(CertsLinq2DbMapping.Schema);
    return new DataConnection(options);
  }

  public Task<State> LoadOrCreateAsync(Guid sessionId, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    using var db = CreateConnection();
    var now = DateTimeOffset.UtcNow;
    var row = db.GetTable<AcmeSessionDto>()
      .Where(x => x.SessionId == sessionId && x.ExpiresAtUtc > now)
      .FirstOrDefault();
    if (row == null)
      return Task.FromResult(new State());
    try {
      return Task.FromResult(AcmeSessionJsonSerializer.FromJson(row.PayloadJson));
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Failed to deserialize ACME session {SessionId}; starting empty state.", sessionId);
      return Task.FromResult(new State());
    }
  }

  public Task PersistAsync(Guid sessionId, State state, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    var json = AcmeSessionJsonSerializer.ToJson(state);
    var now = DateTimeOffset.UtcNow;
    var expires = now.Add(SessionTtl);
    using var db = CreateConnection();
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
    return Task.CompletedTask;
  }
}
