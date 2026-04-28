using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

namespace MaksIT.CertsUI.Engine.Services;

/// <summary>Loads and persists per-browser ACME <see cref="State"/> so any replica can continue the flow.</summary>
public interface IAcmeSessionStore {
  Task<State> LoadOrCreateAsync(Guid sessionId, CancellationToken cancellationToken = default);
  Task PersistAsync(Guid sessionId, State state, CancellationToken cancellationToken = default);
}
