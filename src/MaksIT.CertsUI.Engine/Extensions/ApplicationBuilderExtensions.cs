using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaksIT.CertsUI.Engine.Extensions;

/// <summary>
/// DB migrations run in <c>Program.cs</c> via <see cref="ServiceCollectionExtensions.EnsureCertsEngineMigratedAsync"/> before <c>RunAsync</c>.
/// This method is a no-op kept for backward compatibility with older host wiring.
/// </summary>
public static class ApplicationBuilderExtensions {
  public static void AddCertsEngineMigrations(this IHost host) {
    // No-op: see Program.cs (migrations) and InitializationHostedService (identity bootstrap under lease).
  }
}
