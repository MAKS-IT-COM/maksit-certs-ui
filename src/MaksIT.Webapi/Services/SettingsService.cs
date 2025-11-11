using Microsoft.Extensions.Options;
using MaksIT.Results;
using MaksIT.Core.Extensions;
using MaksIT.Core.Threading;
using MaksIT.Webapi.Domain;
using MaksIT.Webapi.Dto;
using MaksIT.Webapi.Abstractions.Services;


namespace MaksIT.Webapi.Services;

public interface ISettingsService {
  Task<Result<Settings?>> LoadAsync();
  Task<Result> SaveAsync(Settings settings);
}

public class SettingsService(
  ILogger<SettingsService> logger,
  IOptions<Configuration> appSettings
) : ServiceBase(logger, appSettings), ISettingsService, IDisposable {

  private readonly LockManager _lockManager = new LockManager();
  private readonly string _settingsPath = appSettings.Value.SettingsFile;

  #region Internal I/O

  private async Task<Result<Settings?>> LoadInternalAsync() {
    try {
      if (!File.Exists(_settingsPath))
        return Result<Settings?>.Ok(new Settings());

      var json = await File.ReadAllTextAsync(_settingsPath);
      var settingsDto = json.ToObject<SettingsDto>();
      if (settingsDto == null)
        return Result<Settings?>.InternalServerError(new Settings(), "Settings file is invalid or empty.");

      var settings = new Settings {
        Init = settingsDto.Init,
        Users = [.. settingsDto.Users.Select(userDto =>  new User(userDto.Id)
                  .SetName(userDto.Name)
                  .SetSaltedHash(userDto.Salt, userDto.Hash)
                  .SetJwtTokens([.. userDto.JwtTokens.Select(jtDto =>
                    new JwtToken(jtDto.Id)
                      .SetAccessTokenData(jtDto.Token, jtDto.IssuedAt, jtDto.ExpiresAt)
                      .SetRefreshTokenData(jtDto.RefreshToken, jtDto.RefreshTokenExpiresAt)
                  )])
                  .SetLastLogin(userDto.LastLogin)
                )]
      };
      return Result<Settings?>.Ok(settings);
    }
    catch (Exception ex) {
      var message = "Error loading settings file.";
      _logger.LogError(ex, message);
      return Result<Settings?>.InternalServerError(null, [message, .. ex.ExtractMessages()]);
    }
  }

  private async Task<Result> SaveInternalAsync(Settings settings) {
    try {
      var settingsDto = new SettingsDto {
        Init = settings.Init,
        Users = [.. settings.Users.Select(u => new UserDto {
          Id = u.Id,
          Name = u.Name,
          Salt = u.Salt,
          Hash = u.Hash,
          JwtTokens = [.. u.JwtTokens.Select(jt => new JwtTokenDto {
            Id = jt.Id,
            Token = jt.Token,
            ExpiresAt = jt.ExpiresAt,
            IssuedAt = jt.IssuedAt,
            RefreshToken = jt.RefreshToken,
            RefreshTokenExpiresAt = jt.RefreshTokenExpiresAt,
            IsRevoked = jt.IsRevoked
          })],
          LastLogin = u.LastLogin,
        })]
      };

      await File.WriteAllTextAsync(_settingsPath, settingsDto.ToJson());
      _logger.LogInformation("Settings file saved.");
      return Result.Ok();
    }
    catch (Exception ex) {
      var message = "Error saving settings file.";
      _logger.LogError(ex, message);
      return Result.InternalServerError([message, .. ex.ExtractMessages()]);
    }
  }

  #endregion

  public async Task<Result<Settings?>> LoadAsync() {
    return await _lockManager.ExecuteWithLockAsync(() => LoadInternalAsync());
  }

  public async Task<Result> SaveAsync(Settings settings) {
    return await _lockManager.ExecuteWithLockAsync(() => SaveInternalAsync(settings));
  }

  public void Dispose() {
    _lockManager.Dispose();
  }
}
