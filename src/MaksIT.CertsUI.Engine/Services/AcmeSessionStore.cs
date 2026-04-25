using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using Microsoft.Extensions.Caching.Memory;

namespace MaksIT.CertsUI.Engine.Services;

/// <summary>
/// In-memory cache of per-session <see cref="State"/> for ACME flows (directory, account, current order, challenges).
/// </summary>
public sealed class AcmeSessionStore {
  private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

  private readonly IMemoryCache _cache;

  public AcmeSessionStore(IMemoryCache cache) => _cache = cache;

  public State GetOrCreate(Guid sessionId) {
    if (!_cache.TryGetValue(sessionId, out State? state) || state is null) {
      state = new State();
      _cache.Set(sessionId, state, SessionTtl);
    }
    return state;
  }
}
