using System.IO.Compression;
using Microsoft.Extensions.Options;
using MaksIT.Core.Extensions;
using MaksIT.Core.Threading;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Results;
using MaksIT.Webapi.Abstractions.Services;


namespace MaksIT.Webapi.Services;

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
 IOptions<Configuration> appSettings
) : ServiceBase(
  logger,
  appSettings
), ICacheService, IDisposable {

  private readonly string _cacheDirectory = appSettings.Value.CacheFolder;
  private readonly LockManager _lockManager = new();
  private readonly string tmpDir = "/tmp";

  public async Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var accountIds = GetCachedAccounts();
      var caches = new List<RegistrationCache>();
      foreach (var accountId in accountIds) {
        var cacheFilePath = GetCacheFilePath(accountId);
        if (!File.Exists(cacheFilePath)) {
          logger.LogWarning($"Cache file not found for account {accountId}");
          continue;
        }
        var json = await File.ReadAllTextAsync(cacheFilePath);
        if (string.IsNullOrEmpty(json)) {
          logger.LogWarning($"Cache file is empty for account {accountId}");
          continue;
        }
        var cache = json.ToObject<RegistrationCache>();
        if (cache != null)
          caches.Add(cache);
      }
      return Result<RegistrationCache[]?>.Ok(caches.ToArray());
    });
  }

  public async Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var cacheFilePath = GetCacheFilePath(accountId);
      if (!File.Exists(cacheFilePath)) {
        var message = $"Cache file not found for account {accountId}";
        logger.LogWarning(message);
        return Result<RegistrationCache?>.InternalServerError(null, message);
      }
      var json = await File.ReadAllTextAsync(cacheFilePath);
      if (string.IsNullOrEmpty(json)) {
        var message = $"Cache file is empty for account {accountId}";
        logger.LogWarning(message);
        return Result<RegistrationCache?>.InternalServerError(null, message);
      }
      var cache = json.ToObject<RegistrationCache>();
      return Result<RegistrationCache?>.Ok(cache);
    });
  }

  public async Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var cacheFilePath = GetCacheFilePath(accountId);
      var json = cache.ToJson();
      await File.WriteAllTextAsync(cacheFilePath, json);
      logger.LogInformation($"Cache file saved for account {accountId}");
      return Result.Ok();
    });
  }

  public async Task<Result<byte[]>> DownloadCacheZipAsync() {
    try {
      if (!Directory.Exists(_cacheDirectory)) {
        var message = "Cache directory not found.";
        logger.LogWarning(message);
        return Result<byte[]>.NotFound(null, message);
      }

      var zipPath = GetTempZipPath("cache");
      EnsureTempDirAndDeleteFile(zipPath);
      ZipFile.CreateFromDirectory(_cacheDirectory, zipPath);
      var zipBytes = await File.ReadAllBytesAsync(zipPath);
      File.Delete(zipPath);
      logger.LogInformation("Cache zipped to {ZipPath}", zipPath);
      return Result<byte[]>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating or reading cache zip file.";
      logger.LogError(ex, message);
      return Result<byte[]>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId) {
    try {
      var cacheFilePath = GetCacheFilePath(accountId);
      if (!File.Exists(cacheFilePath)) {
        var message = $"Cache file not found for account {accountId}.";
        logger.LogWarning(message);
        return Result<byte[]?>.NotFound(null, message);
      }
      var zipPath = GetTempZipPath($"account_cache_{accountId}");
      EnsureTempDirAndDeleteFile(zipPath);
      using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
        zipArchive.CreateEntryFromFile(cacheFilePath, Path.GetFileName(cacheFilePath));
      }
      var zipBytes = await File.ReadAllBytesAsync(zipPath);
      File.Delete(zipPath);
      logger.LogInformation("Account cache zipped to {ZipPath}", zipPath);
      return Result<byte[]?>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating or reading account cache zip file.";
      logger.LogError(ex, message);
      return Result<byte[]?>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result> UploadCacheZipAsync(byte[] zipBytes) {
    try {
      var zipPath = GetTempZipPath("upload_cache");
      EnsureTempDirAndDeleteFile(zipPath);
      await File.WriteAllBytesAsync(zipPath, zipBytes);
      ZipFile.ExtractToDirectory(zipPath, _cacheDirectory, true);
      File.Delete(zipPath);
      logger.LogInformation("Cache unzipped from {ZipPath}", zipPath);
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error uploading or extracting cache zip file.";
      logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes) {
    try {
      var zipPath = GetTempZipPath($"upload_account_cache_{accountId}");
      EnsureTempDirAndDeleteFile(zipPath);
      await File.WriteAllBytesAsync(zipPath, zipBytes);
      using (var zipArchive = ZipFile.OpenRead(zipPath)) {
        foreach (var entry in zipArchive.Entries) {
          var destinationPath = Path.Combine(_cacheDirectory, entry.FullName);
          entry.ExtractToFile(destinationPath, true);
        }
      }
      File.Delete(zipPath);
      logger.LogInformation("Account cache unzipped from {ZipPath}", zipPath);
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error uploading or extracting account cache zip file.";
      logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result> DeleteCacheAsync() {
    return await _lockManager.ExecuteWithLockAsync(() => {
      try {
        if (Directory.Exists(_cacheDirectory)) {
          // Delete all files
          foreach (var file in Directory.GetFiles(_cacheDirectory)) {
            File.Delete(file);
          }
          // Delete all subdirectories
          foreach (var dir in Directory.GetDirectories(_cacheDirectory)) {
            Directory.Delete(dir, true);
          }
          logger.LogInformation("Cache directory contents cleared.");
        }
        else {
          logger.LogWarning("Cache directory not found to clear.");
        }
        return Result.Ok();
      }
      catch (Exception ex) {
        var message = "Error clearing cache directory contents.";
        logger.LogError(ex, message);
        return Result.InternalServerError([message, .. ex.ExtractMessages()]);
      }
    });
  }

  public async Task<Result> DeleteAccountCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(() => {
      var cacheFilePath = GetCacheFilePath(accountId);
      if (File.Exists(cacheFilePath)) {
        File.Delete(cacheFilePath);
        logger.LogInformation($"Cache file deleted for account {accountId}");
      }
      else {
        logger.LogWarning($"Cache file not found for account {accountId}");
      }
      return Task.FromResult(Result.Ok());
    });
  }

  #region Helpers
  /// <summary>
  /// Generates the cache file path for the given account ID.
  /// </summary>
  private string GetCacheFilePath(Guid accountId) {
    return Path.Combine(_cacheDirectory, $"{accountId}.json");
  }

  private Guid[] GetCachedAccounts() {
    return GetCacheFilesPaths().Select(x => Path.GetFileNameWithoutExtension(x).ToGuid()).Where(x => x != Guid.Empty).ToArray();
  }

  private string[] GetCacheFilesPaths() {
    return Directory.GetFiles(_cacheDirectory);
  }

  private string GetTempZipPath(string prefix) {
    var zipName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
    return Path.Combine(tmpDir, zipName);
  }

  private void EnsureTempDirAndDeleteFile(string filePath) {
    Directory.CreateDirectory(tmpDir);
    if (File.Exists(filePath))
      File.Delete(filePath);
  }
  #endregion

  public void Dispose() {
    _lockManager.Dispose();
  }
}
