using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Infrastructure;

public interface IRuntimeLeaseService {
  Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default);
  Task<Result> ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default);
}
