using System.IO.Compression;
using Microsoft.Extensions.Logging;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Domain-level registration cache operations (zip + row persistence). Host <see cref="MaksIT.CertsUI.Services.CacheService"/> delegates here.
/// </summary>
public sealed class RegistrationCacheDomainService(
  ILogger<RegistrationCacheDomainService> logger,
  IRegistrationCachePersistanceService registrationCachePersistence
) : IRegistrationCacheDomainService {

  private readonly ILogger<RegistrationCacheDomainService> _logger = logger;
  private readonly IRegistrationCachePersistanceService _persistence = registrationCachePersistence;

  public Task<Result<RegistrationCache[]?>> LoadAllAsync(CancellationToken cancellationToken = default) =>
    _persistence.LoadAllAsync(cancellationToken);

  public Task<Result<RegistrationCache?>> LoadAsync(Guid accountId, CancellationToken cancellationToken = default) =>
    _persistence.LoadAsync(accountId, cancellationToken);

  public Task<Result> SaveAsync(Guid accountId, RegistrationCache cache, CancellationToken cancellationToken = default) =>
    _persistence.SaveAsync(accountId, cache, cancellationToken);

  public Task<Result> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    _persistence.DeleteAllAsync(cancellationToken);

  public Task<Result> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default) =>
    _persistence.DeleteAsync(accountId, cancellationToken);

  public async Task<Result<byte[]>> DownloadCacheZipAsync(CancellationToken cancellationToken = default) {
    try {
      var allResult = await _persistence.LoadAllAsync(cancellationToken).ConfigureAwait(false);
      if (!allResult.IsSuccess || allResult.Value == null)
        return Result<byte[]>.InternalServerError(null, allResult.Messages?.ToArray() ?? ["Could not load registration caches."]);

      var rows = allResult.Value;
      using var ms = new MemoryStream();
      if (rows.Length == 0) {
        using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }
        return Result<byte[]>.Ok(ms.ToArray());
      }
      using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) {
        foreach (var row in rows) {
          var entry = zip.CreateEntry($"{row.AccountId}.json");
          using var entryStream = entry.Open();
          using var writer = new StreamWriter(entryStream);
          writer.Write(row.ToJson());
        }
      }
      var zipBytes = ms.ToArray();
      _logger.LogInformation("Exported {Count} registration caches to zip.", rows.Length);
      return Result<byte[]>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating registration cache zip.";
      _logger.LogError(ex, message);
      return Result<byte[]>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId, CancellationToken cancellationToken = default) {
    try {
      var readResult = await _persistence.LoadAsync(accountId, cancellationToken).ConfigureAwait(false);
      if (!readResult.IsSuccess || readResult.Value == null) {
        var message = $"Registration cache not found for account {accountId}.";
        _logger.LogWarning(message);
        return Result<byte[]?>.NotFound(null, message);
      }
      var row = readResult.Value;
      using var ms = new MemoryStream();
      using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) {
        var entry = zip.CreateEntry($"{accountId}.json");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream);
        writer.Write(row.ToJson());
      }
      var zipBytes = ms.ToArray();
      _logger.LogInformation("Account registration cache zipped for {AccountId}", accountId);
      return Result<byte[]?>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating account registration cache zip.";
      _logger.LogError(ex, message);
      return Result<byte[]?>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public Task<Result> UploadCacheZipAsync(byte[] zipBytes, CancellationToken cancellationToken = default) {
    try {
      using var ms = new MemoryStream(zipBytes);
      using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
      return ImportZipEntriesAsync(zip.Entries, null, cancellationToken);
    }
    catch (Exception ex) {
      var message = "Error reading or importing registration cache zip.";
      _logger.LogError(ex, message);
      return Task.FromResult(Result.InternalServerError([message, .. ex.ExtractMessages()]));
    }
  }

  public async Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes, CancellationToken cancellationToken = default) {
    try {
      using var ms = new MemoryStream(zipBytes);
      using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
      var import = await ImportZipEntriesAsync(zip.Entries, accountId, cancellationToken).ConfigureAwait(false);
      if (!import.IsSuccess)
        return import;

      return Result.Ok(import.Messages?.ToArray() ?? ["Imported account registration cache zip."]);
    }
    catch (Exception ex) {
      var message = "Error reading or importing account registration cache zip.";
      _logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  private async Task<Result> ImportZipEntriesAsync(IReadOnlyList<ZipArchiveEntry> entries, Guid? enforcedAccountId, CancellationToken cancellationToken) {
    var processedJsonEntries = 0;

    foreach (var entry in entries) {
      cancellationToken.ThrowIfCancellationRequested();
      if (string.IsNullOrEmpty(entry.Name) || !entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        continue;

      processedJsonEntries++;

      var name = Path.GetFileNameWithoutExtension(entry.Name);
      if (!Guid.TryParse(name, out var id))
        return Result.BadRequest($"Invalid cache entry name '{entry.Name}'. Expected '<accountId>.json'.");

      if (enforcedAccountId != null && id != enforcedAccountId.Value)
        return Result.BadRequest($"Account upload accepts only '{enforcedAccountId}'. Found '{id}' in '{entry.Name}'.");

      using var stream = entry.Open();
      using var reader = new StreamReader(stream);
      var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
      if (string.IsNullOrWhiteSpace(json))
        return Result.BadRequest($"Cache entry '{entry.Name}' is empty.");

      var payload = json.ToObject<RegistrationCachePayloadDocument>();
      if (payload == null) {
        _logger.LogWarning("Skipping zip entry {Name}: invalid JSON.", entry.FullName);
        return Result.BadRequest($"Cache entry '{entry.Name}' has invalid JSON.");
      }

      var cache = new RegistrationCache {
        Id = payload.Id,
        AccountId = payload.Id,
        Description = payload.Description ?? "",
        Contacts = payload.Contacts ?? [],
        IsStaging = payload.IsStaging,
        ChallengeType = payload.ChallengeType ?? "",
        IsDisabled = payload.IsDisabled,
        AccountKey = payload.AccountKey,
        Key = payload.Key,
        Location = payload.Location,
        CachedCerts = payload.CachedCerts,
        AcmeRenewalNotBeforeUtcByHostname = payload.AcmeRenewalNotBeforeUtcByHostname
      };

      var save = await _persistence.SaveAsync(id, cache, cancellationToken).ConfigureAwait(false);
      if (!save.IsSuccess)
        return save;
    }

    if (enforcedAccountId != null && processedJsonEntries != 1)
      return Result.BadRequest($"Account upload requires exactly one JSON entry named '{enforcedAccountId}.json'. Found {processedJsonEntries}.");

    var message = $"Imported registration caches from zip ({processedJsonEntries} entries).";
    _logger.LogInformation(message);
    return Result.Ok(message);
  }
}
