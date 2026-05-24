using Microsoft.Extensions.DependencyInjection;

namespace MaksIT.CertsUI.Client;

/// <summary>Extension methods for registering <see cref="ICertsUIClient"/> in DI.</summary>
public static class ServiceCollectionExtensions {
  /// <summary>Adds <see cref="ICertsUIClient"/> and configures a named <see cref="HttpClient"/> with API key.</summary>
  public static IServiceCollection AddCertsUIClient(this IServiceCollection services, Action<CertsUIClientOptions> configureOptions) {
    services.Configure(configureOptions);
    services.AddHttpClient<ICertsUIClient, CertsUIClient>();
    return services;
  }

  /// <summary>Adds <see cref="ICertsUIClient"/> with explicit base address and API key.</summary>
  public static IServiceCollection AddCertsUIClient(this IServiceCollection services, string baseAddress, string apiKey) {
    services.AddCertsUIClient(opts => {
      opts.BaseAddress = baseAddress;
      opts.ApiKey = apiKey;
    });
    return services;
  }
}
