using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Extensions;
using MaksIT.CertsUI.Engine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace MaksIT.CertsUI.Tests.Infrastructure;

/// <summary>
/// One PostgreSQL container per test collection; runs FluentMigrator (Linq2DB stack, Vault parity).
/// </summary>
public class PostgresCacheFixture : IAsyncLifetime, IDisposable {

  PostgreSqlContainer? _container;
  ServiceProvider? _provider;

  public ICertsDataConnectionFactory ConnectionFactory { get; private set; } = null!;
  public WebApiTestFixture Config { get; private set; } = null!;

  public async Task InitializeAsync() {
    _container = new PostgreSqlBuilder()
      .WithImage("postgres:16-alpine")
      .Build();
    await _container.StartAsync();

    var cs = _container.GetConnectionString();

    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole());
    var testIdentity = new TestIdentityDomainConfiguration(
      "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
      "tests",
      "tests",
      60,
      7,
      "test-pepper");
    services.AddSingleton<IIdentityDomainConfiguration>(testIdentity);
    services.AddSingleton<ITwoFactorSettingsConfiguration>(testIdentity);
    services.AddSingleton<IDefaultAdminBootstrapConfiguration>(testIdentity);
    services.AddCertsEngine(new CertsEngineConfiguration {
      ConnectionString = cs,
      LetsEncryptProduction = "https://acme-v02.api.letsencrypt.org/directory",
      LetsEncryptStaging = "https://acme-staging-v02.api.letsencrypt.org/directory",
    });
    _provider = services.BuildServiceProvider();
    await using (var scope = _provider.CreateAsyncScope()) {
      var run = scope.ServiceProvider.GetRequiredService<IRunMigrationsService>();
      await run.RunAsync();
    }

    ConnectionFactory = _provider.GetRequiredService<ICertsDataConnectionFactory>();
    Config = new WebApiTestFixture();
  }

  public async Task DisposeAsync() {
    Config.Dispose();
    if (_provider != null)
      await _provider.DisposeAsync();
    if (_container != null)
      await _container.DisposeAsync();
  }

  public void Dispose() { }
}
