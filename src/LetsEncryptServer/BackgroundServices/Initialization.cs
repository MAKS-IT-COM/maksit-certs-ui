using Microsoft.Extensions.Options;
using MaksIT.LetsEncryptServer.Domain;
using MaksIT.LetsEncryptServer.Services;


namespace MaksIT.LetsEncryptServer.BackgroundServices {

  public class Initialization : BackgroundService {
    private readonly IServiceProvider _serviceProvider;
    private readonly Configuration _appSettings;
    private readonly ISettingsService _settingsService;

    public Initialization(
      IOptions<Configuration> appSettings,
      IServiceProvider serviceProvider,
      ISettingsService settingsService
    ) {
      _appSettings = appSettings.Value;
      _serviceProvider = serviceProvider;
      _settingsService = settingsService;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      // using var scope = _serviceProvider.CreateScope();
      // TODO: Add your user initialization logic here.
      // Example:
      // var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
      // await userService.InitializeUsersAsync();

      var dataDir = Path.Combine(Path.DirectorySeparatorChar.ToString(), "data");
      var settingsPath = Path.Combine(dataDir, "settings.json");

      // Ensure the data directory exists
      if (!Directory.Exists(dataDir)) {
        Directory.CreateDirectory(dataDir);
      }

      var loadSettingsResult = await _settingsService.LoadAsync();
      if (!loadSettingsResult.IsSuccess || loadSettingsResult.Value == null) {
        throw new Exception("Failed to load settings.");
      }

      var settings = loadSettingsResult.Value;

      if (!settings.Init) {
        var initializeResult = settings.Initialize(_appSettings.Auth.Pepper);
        if (!initializeResult.IsSuccess || initializeResult.Value == null)
          throw new Exception(string.Join(", ", initializeResult.Messages));

        settings = initializeResult.Value;

        var saveResult = await _settingsService.SaveAsync(settings);
        if (!saveResult.IsSuccess) {
          throw new Exception("Failed to save initialized settings.");
        }
      }


      await Task.CompletedTask;
    }
  }
}
