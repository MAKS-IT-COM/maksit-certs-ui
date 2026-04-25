using MaksIT.CertsUI;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Tests.Infrastructure;

/// <summary>
/// Creates a disposable temp workspace and <see cref="IOptions{Configuration}"/> with valid auth and paths.
/// </summary>
public sealed class WebApiTestFixture : IDisposable
{
    public string Root { get; }
    public IOptions<Configuration> AppOptions { get; }

    public WebApiTestFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "maksit-webapi-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        var dataFolder = Path.Combine(Root, "data");
        Directory.CreateDirectory(dataFolder);
        var acmeFolder = Path.Combine(Root, "acme");
        Directory.CreateDirectory(acmeFolder);

        var configuration = new Configuration
        {
            CertsUIEngineConfiguration = new CertsUIEngineConfiguration
            {
                Admin = new AdminUser
                {
                    Username = "admin",
                    Password = "password"
                },
                JwtSettingsConfiguration = new JwtSettingsConfiguration
                {
                    JwtSecret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                    Issuer = "tests",
                    Audience = "tests",
                    ExpiresIn = 60,
                    RefreshTokenExpiresIn = 7,
                    PasswordPepper = "test-pepper-value-for-unit-tests"
                },
                TwoFactorSettingsConfiguration = new TwoFactorSettingsConfiguration
                {
                    Label = "CertsUI",
                    Issuer = "MaksIT.CertsUI",
                    TimeTolerance = 1
                },
                Production = "https://acme-v02.api.letsencrypt.org/directory",
                Staging = "https://acme-staging-v02.api.letsencrypt.org/directory",
                AcmeFolder = acmeFolder,
                DataFolder = dataFolder,
                Agent = new Agent
                {
                    AgentHostname = "http://127.0.0.1",
                    AgentPort = 9,
                    AgentKey = "test-key",
                    ServiceToReload = "nginx"
                }
            }
        };

        AppOptions = Microsoft.Extensions.Options.Options.Create(configuration);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch
        {
            // best-effort cleanup of temp dir
        }
    }
}
