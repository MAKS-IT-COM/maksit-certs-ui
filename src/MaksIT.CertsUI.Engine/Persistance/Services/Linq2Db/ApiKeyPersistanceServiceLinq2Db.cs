using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Mappers;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IAPIKeyPersistanceService"/>.
/// </summary>
public sealed class ApiKeyPersistanceServiceLinq2Db(
  ILogger<ApiKeyPersistanceServiceLinq2Db> logger,
  ICertsDataConnectionFactory connectionFactory
) : IAPIKeyPersistanceService {

  private readonly ILogger<ApiKeyPersistanceServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;

  public Task<Result<Guid>> TryValidateLegacyKeyHashAsync(string keyHashHex, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var row = db.GetTable<ApiKeyDto>()
        .FirstOrDefault(k =>
          k.KeyHashHex == keyHashHex &&
          k.RevokedAtUtc == null &&
          (k.KeySalt == null || k.KeySalt == string.Empty));
      if (row == null)
        return Task.FromResult(Result<Guid>.Forbidden(default, "Invalid API key."));
      if (row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value <= DateTime.UtcNow)
        return Task.FromResult(Result<Guid>.Forbidden(default, "API key is expired."));
      return Task.FromResult(Result<Guid>.Ok(row.Id));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error validating API key hash.");
      return Task.FromResult(Result<Guid>.InternalServerError(default, ["An error occurred while validating the API key.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<ApiKey?>> ReadByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var row = db.GetTable<ApiKeyDto>().FirstOrDefault(k => k.Id == id);
      if (row == null)
        return Task.FromResult(Result<ApiKey?>.NotFound(null, "API key not found."));
      return Task.FromResult(Result<ApiKey?>.Ok(ApiKeyMapper.MapToDomain(row)));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading API key with ID {ApiKeyId}", id);
      return Task.FromResult(Result<ApiKey?>.InternalServerError(null, ["An error occurred while retrieving the API key.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> InsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(apiKey);

    try {
      using var db = _connectionFactory.Create();
      db.Insert(ApiKeyMapper.MapToDto(apiKey));
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error inserting API key {ApiKeyId}", apiKey.Id);
      return Task.FromResult(Result.InternalServerError(["An error occurred while saving the API key.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(apiKey);

    try {
      using var db = _connectionFactory.Create();
      var next = ApiKeyMapper.MapToDto(apiKey);
      var existing = db.GetTable<ApiKeyDto>().FirstOrDefault(k => k.Id == next.Id);
      if (existing == null)
        return Task.FromResult(Result.NotFound("API key not found."));
      existing.Description = next.Description;
      existing.ExpiresAtUtc = next.ExpiresAtUtc;
      existing.KeySalt = next.KeySalt;
      existing.KeyHashHex = next.KeyHashHex;
      existing.CreatedAtUtc = next.CreatedAtUtc;
      existing.RevokedAtUtc = next.RevokedAtUtc;
      db.Update(existing);
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error updating API key {ApiKeyId}", apiKey.Id);
      return Task.FromResult(Result.InternalServerError(["An error occurred while updating the API key.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();

    try {
      using var db = _connectionFactory.Create();
      var deleted = db.GetTable<ApiKeyDto>().Where(k => k.Id == id).Delete();
      if (deleted == 0)
        return Task.FromResult(Result.NotFound("API key not found."));
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting API key {ApiKeyId}", id);
      return Task.FromResult(Result.InternalServerError(["An error occurred while deleting the API key.", .. ex.ExtractMessages()]));
    }
  }
}
