using Microsoft.Extensions.DependencyInjection;

using Serilog;


using Microsoft.Extensions.Configuration;
using SSHProvider;

namespace MaksIT.Tests.SSHProviderTests.Abstractions {
  public abstract class ServicesBase : ConfigurationBase {

    public ServicesBase() : base() { }

    protected override void ConfigureServices(IServiceCollection services) {
      // configure strongly typed settings objects
      var appSettingsSection = Configuration.GetSection("Configuration");
      services.Configure<Configuration>(appSettingsSection);
      var appSettings = appSettingsSection.Get<Configuration>();

      #region configurazione logging
      services.AddLogging(configure => {
        configure.AddSerilog(new LoggerConfiguration()
            //.ReadFrom.Configuration(_configuration)
            .CreateLogger());
      });
      #endregion

    }
  }
}