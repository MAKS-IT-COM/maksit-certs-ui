using MaksIT.Core.Threading;
using MaksIT.LetsEncryptServer.Domain;
using MaksIT.LetsEncryptServer.Dto;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace MaksIT.LetsEncryptServer.Services;

public interface ISettingsService {
    Task<Result<Settings>> LoadAsync();
    Task<Result> SaveAsync(Settings settings);
}

public class SettingsService : ISettingsService, IDisposable {
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private readonly LockManager _lockManager;

    public SettingsService(
        ILogger<SettingsService> logger,
        IOptions<Configuration> appSettings
    ) {
        _logger = logger;
        _settingsPath = appSettings.Value.SettingsFile;
        _lockManager = new LockManager();
    }

    #region Internal I/O

    private async Task<Result<Settings>> LoadInternalAsync() {
        try {
            if (!File.Exists(_settingsPath))
                return Result<Settings>.Ok(new Settings());

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settingsDto = json.ToObject<SettingsDto>();
            if (settingsDto == null)
                return Result<Settings>.InternalServerError(new Settings(), "Settings file is invalid or empty.");

            var settings = new Settings {
                Init = settingsDto.Init,
                Users = [.. settingsDto.Users.Select(userDto => new User(userDto.Id.ToGuid(), userDto.Name).SeltSaltedHash(userDto.Salt, userDto.Hash))]
            };
            return Result<Settings>.Ok(settings);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error loading settings file.");
            return Result<Settings>.InternalServerError(new Settings(), ex.Message);
        }
    }

    private async Task<Result> SaveInternalAsync(Settings settings) {
        try {
            var settingsDto = new SettingsDto {
                Init = settings.Init,
                Users = [.. settings.Users.Select(u => new UserDto {
                    Id = u.Id.ToString(),
                    Name = u.Name,
                    Salt = u.Salt,
                    Hash = u.Hash
                })]
            };
            await File.WriteAllTextAsync(_settingsPath, settingsDto.ToJson());
            _logger.LogInformation("Settings file saved.");
            return Result.Ok();
        } catch (Exception ex) {
            _logger.LogError(ex, "Error saving settings file.");
            return Result.InternalServerError(ex.Message);
        }
    }

    #endregion

    public async Task<Result<Settings>> LoadAsync() {
        return await _lockManager.ExecuteWithLockAsync(() => LoadInternalAsync());
    }

    public async Task<Result> SaveAsync(Settings settings) {
        return await _lockManager.ExecuteWithLockAsync(() => SaveInternalAsync(settings));
    }

    public void Dispose() {
        _lockManager.Dispose();
    }
}
