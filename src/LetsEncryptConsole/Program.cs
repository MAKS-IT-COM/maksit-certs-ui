using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using MaksIT.LetsEncryptConsole.Services;
using MaksIT.LetsEncrypt.Extensions;

namespace MaksIT.LetsEncryptConsole;

class Program {
  private static readonly IConfiguration _configuration = InitConfig();

  static void Main(string[] args) {
    // create service collection
    var services = new ServiceCollection();
    ConfigureServices(services);

    // create service provider
    var serviceProvider = services.BuildServiceProvider();

    // entry to run app
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    var app = serviceProvider.GetService<App>();
    app.Run(args).Wait();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
  }

  public static void ConfigureServices(IServiceCollection services) {

    var configurationSection = _configuration.GetSection("Configuration");
    services.Configure<Configuration>(configurationSection);
    var appSettings = configurationSection.Get<Configuration>();

    #region Configure logging
    services.AddLogging(configure => {
      configure.AddSerilog(new LoggerConfiguration()
        .ReadFrom.Configuration(_configuration)
        .CreateLogger());
    });
    #endregion

    #region Services
    services.RegisterLetsEncrypt();
    services.AddSingleton<ITerminalService, TerminalService>();
    #endregion

    // add app
    services.AddSingleton<App>();
  }

  private static IConfiguration InitConfig() {
    var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    var configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddEnvironmentVariables();

    if (!string.IsNullOrWhiteSpace(aspNetCoreEnvironment)
        && new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{aspNetCoreEnvironment}.json")).Exists
    )
      configuration.AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", true);
    else
      configuration.AddJsonFile($"appsettings.json", true, true);

    return configuration.Build();
  }
}

