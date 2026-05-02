using MaksIT.CertsUI;
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
/// One PostgreSQL container per test collection; runs FluentMigrator (Linq2DB stack).
/// </summary>
public class PostgresCacheFixture : IAsyncLifetime, IDisposable {

  PostgreSqlContainer? _container;
  ServiceProvider? _provider;

  public ICertsUIDataConnectionFactory ConnectionFactory { get; private set; } = null!;
  public WebApiTestFixture Config { get; private set; } = null!;

  public async Task InitializeAsync() {
    _container = new PostgreSqlBuilder("postgres:16-alpine")
      .Build();
    await _container.StartAsync();

    var cs = _container.GetConnectionString();

    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole());
    services.AddCertsEngine(new CertsEngineConfiguration {
      ConnectionString = cs,
      AutoSyncSchema = true,
      Admin = new AdminUser { Username = "pg-test", Password = "pg-test" },
      JwtSettingsConfiguration = new JwtSettingsConfiguration {
        JwtSecret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        Issuer = "tests",
        Audience = "tests",
        ExpiresIn = 60,
        RefreshTokenExpiresIn = 7,
        PasswordPepper = "test-pepper"
      },
      TwoFactorSettingsConfiguration = new TwoFactorSettingsConfiguration {
        Label = "CertsUI",
        Issuer = "MaksIT.CertsUI",
        TimeTolerance = 1
      },
      Agent = new Agent {
        AgentHostname = "127.0.0.1",
        AgentPort = 1,
        AgentKey = "k",
        ServiceToReload = "nginx"
      },
      Production = "https://acme-v02.api.letsencrypt.org/directory",
      Staging = "https://acme-staging-v02.api.letsencrypt.org/directory",
    });
    _provider = services.BuildServiceProvider();
    await _provider.EnsureCertsEngineMigratedAsync();

    ConnectionFactory = _provider.GetRequiredService<ICertsUIDataConnectionFactory>();
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
