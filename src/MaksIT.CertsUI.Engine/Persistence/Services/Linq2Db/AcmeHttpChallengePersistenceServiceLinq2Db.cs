using LinqToDB;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using MaksIT.Core.Extensions;

namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

public sealed class AcmeHttpChallengePersistenceServiceLinq2Db(
  ILogger<AcmeHttpChallengePersistenceServiceLinq2Db> logger,
  ICertsUIDataConnectionFactory connectionFactory
) : IAcmeHttpChallengePersistenceService {

  public Task<Result> UpsertAsync(string fileName, string tokenValue, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    if (string.IsNullOrWhiteSpace(fileName))
      return Task.FromResult(Result.BadRequest("Challenge file name is required."));
    if (string.IsNullOrEmpty(tokenValue))
      return Task.FromResult(Result.BadRequest("Challenge token value is required."));

    try {
      using var db = connectionFactory.Create();
      var now = DateTimeOffset.UtcNow;
      var row = db.GetTable<AcmeHttpChallengeDto>().FirstOrDefault(x => x.FileName == fileName);
      if (row == null) {
        db.Insert(new AcmeHttpChallengeDto {
          FileName = fileName,
          TokenValue = tokenValue,
          CreatedAtUtc = now
        });
      }
      else {
        row.TokenValue = tokenValue;
        row.CreatedAtUtc = now;
        db.Update(row);
      }

      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      logger.LogError(ex, "Upsert HTTP-01 challenge failed for {FileName}", fileName);
      return Task.FromResult(Result.InternalServerError(["Failed to persist HTTP-01 challenge.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<string?>> GetTokenValueAsync(string fileName, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    if (string.IsNullOrWhiteSpace(fileName))
      return Task.FromResult(Result<string?>.BadRequest(null, "Challenge file name is required."));

    try {
      using var db = connectionFactory.Create();
      var row = db.GetTable<AcmeHttpChallengeDto>().FirstOrDefault(x => x.FileName == fileName);
      if (row == null)
        return Task.FromResult(Result<string?>.NotFound(null, $"Challenge token not found: {fileName}"));

      return Task.FromResult(Result<string?>.Ok(row.TokenValue));
    }
    catch (Exception ex) {
      logger.LogError(ex, "Load HTTP-01 challenge failed for {FileName}", fileName);
      return Task.FromResult(Result<string?>.InternalServerError(null, ["Failed to load HTTP-01 challenge.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<int>> DeleteOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    if (maxAge <= TimeSpan.Zero)
      return Task.FromResult(Result<int>.BadRequest(0, "maxAge must be positive."));

    try {
      using var db = connectionFactory.Create();
      var cutoff = DateTimeOffset.UtcNow - maxAge;
      var deleted = db.GetTable<AcmeHttpChallengeDto>().Where(x => x.CreatedAtUtc < cutoff).Delete();
      return Task.FromResult(Result<int>.Ok(deleted));
    }
    catch (Exception ex) {
      logger.LogError(ex, "Delete old HTTP-01 challenges failed.");
      return Task.FromResult(Result<int>.InternalServerError(0, ["Failed to delete old HTTP-01 challenges.", .. ex.ExtractMessages()]));
    }
  }
}
