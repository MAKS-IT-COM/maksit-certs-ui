using MaksIT.Core.Extensions;
using MaksIT.Core.Threading;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text.Json;

namespace MaksIT.LetsEncryptServer.Services;

public interface ICacheService {
  Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync();
  Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId);
  Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache);
  Task<Result> DeleteFromCacheAsync(Guid accountId);

  Task<Result<byte[]>> DownloadCacheZipAsync();
  Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId);
  Task<Result> UploadCacheZipAsync(byte[] zipBytes);
  Task<Result> UploadAccountCacheZipAsync(Guid accountId, byte[] zipBytes);
  Task<Result> ClearCacheAsync();
}

public class CacheService : ICacheService, IDisposable {
  private readonly ILogger<CacheService> _logger;
  private readonly string _cacheDirectory;
  private readonly LockManager _lockManager;

  private readonly string tmpDir = "/tmp";

  public CacheService(
    ILogger<CacheService> logger,
    IOptions<Configuration> appsettings
  ) {
    _logger = logger;
    _cacheDirectory = appsettings.Value.CacheFolder;
    _lockManager = new LockManager();
  }

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

  #region Cache Operations

  public async Task<Result<RegistrationCache[]?>> LoadAccountsFromCacheAsync() {
    return await _lockManager.ExecuteWithLockAsync(async () => {
      var accountIds = GetCachedAccounts();
      var cacheLoadTasks = accountIds.Select(accountId => LoadFromCacheInternalAsync(accountId)).ToList();

      var caches = new List<RegistrationCache>();
      foreach (var task in cacheLoadTasks) {
        var taskResult = await task;
        if (!taskResult.IsSuccess || taskResult.Value == null) {
          // Depending on how you want to handle partial failures, you might want to return here
          // or continue loading other caches. For now, let's continue.
          continue;
        }

        var registrationCache = taskResult.Value;

        caches.Add(registrationCache);
      }

      return Result<RegistrationCache[]?>.Ok(caches.ToArray());
    });
  }

  private async Task<Result<RegistrationCache?>> LoadFromCacheInternalAsync(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);

    if (!File.Exists(cacheFilePath)) {
      var message = $"Cache file not found for account {accountId}";
      _logger.LogWarning(message);
      return Result<RegistrationCache?>.InternalServerError(null, message);
    }

    var json = await File.ReadAllTextAsync(cacheFilePath);
    if (string.IsNullOrEmpty(json)) {
      var message = $"Cache file is empty for account {accountId}";
      _logger.LogWarning(message);
      return Result<RegistrationCache?>.InternalServerError(null, message);
    }

    var cache = json.ToObject<RegistrationCache>();
    return Result<RegistrationCache?>.Ok(cache);
  }

  private async Task<Result> SaveToCacheInternalAsync(Guid accountId, RegistrationCache cache) {
    var cacheFilePath = GetCacheFilePath(accountId);
    var json = cache.ToJson();
    await File.WriteAllTextAsync(cacheFilePath, json);
    _logger.LogInformation($"Cache file saved for account {accountId}");
    return Result.Ok();
  }

  private Result DeleteFromCacheInternal(Guid accountId) {
    var cacheFilePath = GetCacheFilePath(accountId);
    if (File.Exists(cacheFilePath)) {
      File.Delete(cacheFilePath);
      _logger.LogInformation($"Cache file deleted for account {accountId}");
    }
    else {
      _logger.LogWarning($"Cache file not found for account {accountId}");
    }
    return Result.Ok();
  }

  #endregion


  #region
  private string GetTempZipPath(string prefix)
  {
    var zipName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
    return Path.Combine(tmpDir, zipName);
  }

  private void EnsureTempDirAndDeleteFile(string filePath)
  {
    Directory.CreateDirectory(tmpDir);
    if (File.Exists(filePath))
      File.Delete(filePath);
  }

  public async Task<Result<byte[]>> DownloadCacheZipAsync() {
    try {
      if (!Directory.Exists(_cacheDirectory)) {
        var message = "Cache directory not found.";
        _logger.LogWarning(message);
        return Result<byte[]>.NotFound(null, message);
      }

      var zipPath = GetTempZipPath("cache");
      EnsureTempDirAndDeleteFile(zipPath);
      ZipFile.CreateFromDirectory(_cacheDirectory, zipPath);
      var zipBytes = await File.ReadAllBytesAsync(zipPath);
      File.Delete(zipPath);
      _logger.LogInformation("Cache zipped to {ZipPath}", zipPath);
      return Result<byte[]>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating or reading cache zip file.";
      _logger.LogError(ex, message);
      return Result<byte[]>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result<byte[]?>> DownloadAccountCacheZipAsync(Guid accountId) {
    try {
      var cacheFilePath = GetCacheFilePath(accountId);
      if (!File.Exists(cacheFilePath)) {
        var message = $"Cache file not found for account {accountId}.";
        _logger.LogWarning(message);
        return Result<byte[]?>.NotFound(null, message);
      }
      var zipPath = GetTempZipPath($"account_cache_{accountId}");
      EnsureTempDirAndDeleteFile(zipPath);
      using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
        zipArchive.CreateEntryFromFile(cacheFilePath, Path.GetFileName(cacheFilePath));
      }
      var zipBytes = await File.ReadAllBytesAsync(zipPath);
      File.Delete(zipPath);
      _logger.LogInformation("Account cache zipped to {ZipPath}", zipPath);
      return Result<byte[]?>.Ok(zipBytes);
    }
    catch (Exception ex) {
      var message = "Error creating or reading account cache zip file.";
      _logger.LogError(ex, message);
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
      _logger.LogInformation("Cache unzipped from {ZipPath}", zipPath);
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error uploading or extracting cache zip file.";
      _logger.LogError(ex, message);
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
      _logger.LogInformation("Account cache unzipped from {ZipPath}", zipPath);
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error uploading or extracting account cache zip file.";
      _logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  public async Task<Result> ClearCacheAsync() {
    try {
      if (Directory.Exists(_cacheDirectory)) {
        Directory.Delete(_cacheDirectory, true);
        _logger.LogInformation("Cache directory cleared.");
      }
      else {
        _logger.LogWarning("Cache directory not found to clear.");
      }
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error clearing cache directory.";
      _logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  #endregion

  public async Task<Result<RegistrationCache?>> LoadAccountFromCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(() => LoadFromCacheInternalAsync(accountId));
  }

  public async Task<Result> SaveToCacheAsync(Guid accountId, RegistrationCache cache) {
    return await _lockManager.ExecuteWithLockAsync(() => SaveToCacheInternalAsync(accountId, cache));
  }

  public async Task<Result> DeleteFromCacheAsync(Guid accountId) {
    return await _lockManager.ExecuteWithLockAsync(() => DeleteFromCacheInternal(accountId));
  }

  public void Dispose() {
    _lockManager.Dispose();
  }
}
