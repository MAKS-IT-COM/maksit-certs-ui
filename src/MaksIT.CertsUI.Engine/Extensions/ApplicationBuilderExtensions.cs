using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaksIT.CertsUI.Engine.Extensions;

/// <summary>
/// DB migrations are handled by FluentMigrator and optional schema sync from InitializationHostedService.
/// This method is a no-op for backward compatibility with host startup.
/// </summary>
public static class ApplicationBuilderExtensions {
  public static void AddCertsEngineMigrations(this IHost host) {
    // No-op: migrations and schema sync run from InitializationHostedService via IRunMigrationsService and ISchemaSyncService.
  }
}
