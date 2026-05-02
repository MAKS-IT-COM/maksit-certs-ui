using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

public interface IAcmeHttpChallengePersistenceService {
  Task<Result> UpsertAsync(string fileName, string tokenValue, CancellationToken cancellationToken = default);
  Task<Result<string?>> GetTokenValueAsync(string fileName, CancellationToken cancellationToken = default);
  Task<Result<int>> DeleteOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}
