using Microsoft.Extensions.DependencyInjection;

using MaksIT.LetsEncrypt.Services;
using Microsoft.Extensions.Configuration;

namespace MaksIT.LetsEncrypt.Extensions;
public static class ServiceCollectionExtensions {
  public static void RegisterLetsEncrypt(this IServiceCollection services, ILetsEncryptConfiguration appSettings) {


    var config = new LetsEncryptConfiguration {
      Staging = appSettings.Staging,
      Production = appSettings.Production
    };

    services.AddSingleton(config);
    services.AddHttpClient<ILetsEncryptService, LetsEncryptService>();
  }
}
