using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistance.Services;

public interface ITermsOfServiceCachePersistenceService {
  Task<Result<TermsOfServiceCacheDto?>> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
  Task<Result> UpsertAsync(TermsOfServiceCacheDto cacheEntry, CancellationToken cancellationToken = default);
}
