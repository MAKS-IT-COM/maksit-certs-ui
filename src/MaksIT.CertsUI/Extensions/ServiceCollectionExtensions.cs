using MaksIT.CertsUI.Mappers;

namespace MaksIT.CertsUI.Extensions;

public static class ServiceCollectionExtensions {

  /// <summary>
  /// Registers all CertsUI response mappers (domain/query -> API response) as scoped services.
  /// </summary>
  public static IServiceCollection AddCertsUIMappers(this IServiceCollection services) {
    services.AddScoped<UserToResponseMapper>();
    services.AddScoped<ApiKeyToResponseMapper>();
    services.AddScoped<AccountToResponseMapper>();

    return services;
  }
}
