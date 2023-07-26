using Microsoft.Extensions.DependencyInjection;

using MaksIT.LetsEncrypt.Services;

namespace MaksIT.LetsEncrypt.Extensions {
  public static class ServiceCollectionExtensions {
    public static void RegisterLetsEncrypt(this IServiceCollection services) {

      services.AddHttpClient<ILetsEncryptService, LetsEncryptService>();
      services.AddSingleton<IJwsService, JwsService>();
    }
  }
}
