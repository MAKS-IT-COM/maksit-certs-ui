using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

//using PecMgr.VaultProvider.Extensions;
//using PecMgr.VaultProvider;
//using PecMgr.Core.Abstractions;

namespace MaksIT.Tests.SSHProviderTests.Abstractions {
  //[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
  public abstract class ConfigurationBase {

    protected IConfiguration Configuration;

    protected ServiceCollection ServiceCollection = new ServiceCollection();

    protected ServiceProvider ServiceProvider { get => ServiceCollection.BuildServiceProvider(); }

    public ConfigurationBase() {
      Configuration = InitConfig();
      ConfigureServices(ServiceCollection);
    }

    protected abstract void ConfigureServices(IServiceCollection services);

    private IConfiguration InitConfig() {
      var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
      var currentDirectory = Directory.GetCurrentDirectory();

      var configurationBuilder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddEnvironmentVariables();

      if (!string.IsNullOrWhiteSpace(aspNetCoreEnvironment) && new FileInfo(Path.Combine(currentDirectory, $"appsettings.{aspNetCoreEnvironment}.json")).Exists)
        configurationBuilder.AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", true);
      else if (new FileInfo(Path.Combine(currentDirectory, "appsettings.json")).Exists)
        configurationBuilder.AddJsonFile("appsettings.json", true, true);
      else
        throw new FileNotFoundException($"Unable to find appsetting.json in {currentDirectory}");

      //var builtConfig = configurationBuilder.Build();
      //var vaultOptions = builtConfig.GetSection("Vault");

      //configurationBuilder.AddVault(options => {
      //  options.Address = vaultOptions["Address"];

      //  options.UnsealKeys = vaultOptions.GetSection("UnsealKeys").Get<List<string>>();

      //  options.AuthMethod = EnumerationStringId.FromValue<AuthenticationMethod>(vaultOptions["AuthMethod"]);
      //  options.AppRoleAuthMethod = vaultOptions.GetSection("AppRoleAuthMethod").Get<AppRoleAuthMethod>();
      //  options.TokenAuthMethod = vaultOptions.GetSection("TokenAuthMethod").Get<TokenAuthMethod>();

      //  options.MountPath = vaultOptions["MountPath"];
      //  options.SecretType = vaultOptions["SecretType"];

      //  options.ConfigurationMappings = vaultOptions.GetSection("ConfigurationMappings").Get<Dictionary<string, string>>();
      //});

      return configurationBuilder.Build();
    }
  }
}
