using System.IO.Compression;
using Microsoft.Extensions.Options;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;

namespace MaksIT.CertsUI.Services;

public interface ICacheService {
  Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync();
  Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId);
  Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<Result<byte[]>> DownloadCacheZipAsync();
  Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId);
  Task<Result> UploadCacheZipAsync(byte[] zipBytes);
  Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes);
  Task<Result> DeleteCacheAsync();
  Task<Result> DeleteAccountCacheAsync(Guid accountId);
}

public class CacheService(
  ILogger<CacheService> logger,
  IOptions<Configuration> appSettings,
  IRegistrationCachePersistanceService registrationCachePersistence
) : ServiceBase(
  logger,
  appSettings
), ICacheService {

  public Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() {
    return registrationCachePersistence.LoadAllAsync();
  }

  public Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) =>
    registrationCachePersistence.LoadAsync(accountId);

  public Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) =>
    registrationCachePersistence.SaveAsync(accountId, cache);

  public async Task<Result<byte[]>> DownloadCacheZipAsync() {
    try {
      var allResult = await registrationCachePersistence.LoadAllAsync();
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
      logger.LogInformation("Exported {Count} registration caches to zip.", rows.Length);
      return Result<byte[]>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating registration cache zip.";
      logger.LogError(ex, message);
      return Result<byte[]>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId) {
    try {
      var readResult = await registrationCachePersistence.LoadAsync(accountId);
      if (!readResult.IsSuccess || readResult.Value == null) {
        var message = $"Registration cache not found for account {accountId}.";
        logger.LogWarning(message);
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
      logger.LogInformation("Account registration cache zipped for {AccountId}", accountId);
      return Result<byte[]?>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating account registration cache zip.";
      logger.LogError(ex, message);
      return Result<byte[]?>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public Task<Result> UploadCacheZipAsync(byte[] zipBytes) {
    try {
      using var ms = new MemoryStream(zipBytes);
      using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
      return ImportZipEntriesAsync(zip.Entries);
    }
    catch (Exception ex) {
      var message = "Error reading or importing registration cache zip.";
      logger.LogError(ex, message);
      return Task.FromResult(Result.InternalServerError([message, .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes) {
    try {
      using var ms = new MemoryStream(zipBytes);
      using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
      return ImportZipEntriesAsync(zip.Entries);
    }
    catch (Exception ex) {
      var message = "Error reading or importing account registration cache zip.";
      logger.LogError(ex, message);
      return Task.FromResult(Result.InternalServerError([message, .. ex.ExtractMessages()]));
    }
  }

  private async Task<Result> ImportZipEntriesAsync(IReadOnlyList<ZipArchiveEntry> entries) {
      foreach (var entry in entries) {
        if (string.IsNullOrEmpty(entry.Name) || !entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
          continue;
        var name = Path.GetFileNameWithoutExtension(entry.Name);
        if (!Guid.TryParse(name, out var id))
          continue;
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(json))
          continue;
        var cache = json.ToObject<RegistrationCache>();
        if (cache == null) {
          logger.LogWarning("Skipping zip entry {Name}: invalid JSON.", entry.FullName);
          continue;
        }
        cache.AccountId = id;
        var save = await registrationCachePersistence.SaveAsync(id, cache);
        if (!save.IsSuccess)
          return save;
      }
      logger.LogInformation("Imported registration caches from zip ({EntryCount} entries).", entries.Count);
      return Result.Ok();
  }

  public Task<Result> DeleteCacheAsync() => registrationCachePersistence.DeleteAllAsync();

  public Task<Result> DeleteAccountCacheAsync(Guid accountId) => registrationCachePersistence.DeleteAsync(accountId);
}
