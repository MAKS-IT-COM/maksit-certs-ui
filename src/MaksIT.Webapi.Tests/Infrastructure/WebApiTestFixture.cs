using MaksIT.Webapi;
using Microsoft.Extensions.Options;

namespace MaksIT.Webapi.Tests.Infrastructure;

/// <summary>
/// Creates a disposable temp workspace and <see cref="IOptions{Configuration}"/> with valid auth and paths.
/// </summary>
public sealed class WebApiTestFixture : IDisposable
{
    public string Root { get; }
    public string SettingsFilePath { get; }
    public string CacheFolderPath { get; }
    public IOptions<Configuration> AppOptions { get; }

    public WebApiTestFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "maksit-webapi-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        SettingsFilePath = Path.Combine(Root, "settings.json");
        CacheFolderPath = Path.Combine(Root, "cache");
        Directory.CreateDirectory(CacheFolderPath);
        var dataFolder = Path.Combine(Root, "data");
        Directory.CreateDirectory(dataFolder);
        var acmeFolder = Path.Combine(Root, "acme");
        Directory.CreateDirectory(acmeFolder);

        var configuration = new Configuration
        {
            Auth = new Auth
            {
                Secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                Issuer = "tests",
                Audience = "tests",
                Expiration = 60,
                RefreshExpiration = 7,
                Pepper = "test-pepper-value-for-unit-tests"
            },
            SettingsFile = SettingsFilePath,
            Production = "https://acme-v02.api.letsencrypt.org/directory",
            Staging = "https://acme-staging-v02.api.letsencrypt.org/directory",
            CacheFolder = CacheFolderPath,
            AcmeFolder = acmeFolder,
            DataFolder = dataFolder,
            Agent = new Agent
            {
                AgentHostname = "http://127.0.0.1",
                AgentPort = 9,
                AgentKey = "test-key",
                ServiceToReload = "nginx"
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
