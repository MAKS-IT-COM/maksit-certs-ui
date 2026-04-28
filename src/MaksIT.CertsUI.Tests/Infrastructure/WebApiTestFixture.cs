using MaksIT.CertsUI;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Tests.Infrastructure;

/// <summary>
/// Creates <see cref="IOptions{Configuration}"/> with valid auth and agent settings for API/domain tests.
/// </summary>
public sealed class WebApiTestFixture : IDisposable
{
    public IOptions<Configuration> AppOptions { get; }

    public WebApiTestFixture()
    {
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

    public void Dispose() { }
}
