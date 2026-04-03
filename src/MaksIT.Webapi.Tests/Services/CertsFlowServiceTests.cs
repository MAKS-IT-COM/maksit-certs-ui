using System.Net;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;
using MaksIT.LetsEncrypt.Services;
using MaksIT.Results;
using MaksIT.Webapi.Services;
using MaksIT.Webapi.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MaksIT.Webapi.Tests.Services;

public sealed class CertsFlowServiceTests
{
    private static CertsFlowService CreateSut(
        WebApiTestFixture fx,
        Mock<ILetsEncryptService> le,
        Mock<ICacheService>? cache = null,
        Mock<IAgentService>? agent = null,
        HttpMessageHandler? httpHandler = null)
    {
        cache ??= new Mock<ICacheService>();
        agent ??= new Mock<IAgentService>();
        var handler = httpHandler ?? new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]) });
        var httpClient = new HttpClient(handler, disposeHandler: true);
        return new CertsFlowService(
            fx.AppOptions,
            NullLogger<CertsFlowService>.Instance,
            httpClient,
            le.Object,
            cache.Object,
            agent.Object);
    }

    [Fact]
    public async Task ConfigureClientAsync_WhenConfigureSucceeds_ReturnsNewSessionId()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.ConfigureClient(It.IsAny<Guid>(), false))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le);

        var result = await sut.ConfigureClientAsync(isStaging: false);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ConfigureClientAsync_WhenConfigureFails_PropagatesFailure()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.ConfigureClient(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.InternalServerError(["configure failed"]));

        var sut = CreateSut(fx, le);

        var result = await sut.ConfigureClientAsync(isStaging: true);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InitAsync_WhenAccountIdNull_CallsInitWithNewAccountId()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.Init(sessionId, It.IsAny<Guid>(), "d", It.Is<string[]>(c => c.Length == 1 && c[0] == "mailto:a@b"), null))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le);

        var result = await sut.InitAsync(sessionId, null, "d", ["mailto:a@b"]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        le.Verify(x => x.Init(sessionId, It.IsAny<Guid>(), "d", It.IsAny<string[]>(), null), Times.Once);
    }

    [Fact]
    public async Task InitAsync_WhenCacheMiss_GeneratesNewAccountId()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var requestedAccount = Guid.NewGuid();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(requestedAccount))
            .ReturnsAsync(Result<RegistrationCache?>.InternalServerError(null, "missing"));

        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.Init(sessionId, It.IsAny<Guid>(), "d", It.IsAny<string[]>(), null))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le, cache);

        var result = await sut.InitAsync(sessionId, requestedAccount, "d", ["mailto:a@b"]);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(requestedAccount, result.Value);
    }

    [Fact]
    public async Task InitAsync_WhenCacheHit_PassesCacheToInit()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "x",
            Contacts = ["mailto:x@y"],
            IsStaging = false,
            ChallengeType = "http-01"
        };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));

        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.Init(sessionId, accountId, "d", It.IsAny<string[]>(), reg))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le, cache);

        var result = await sut.InitAsync(sessionId, accountId, "d", ["mailto:a@b"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(accountId, result.Value);
    }

    [Fact]
    public async Task NewOrderAsync_WhenOrderSucceeds_WritesAcmeTokenFiles()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.NewOrder(sessionId, It.IsAny<string[]>(), "http-01"))
            .ReturnsAsync(Result<Dictionary<string, string>?>.Ok(new Dictionary<string, string>
            {
                ["example.com"] = "tokenPart.rest.of.token"
            }));

        var sut = CreateSut(fx, le);

        var result = await sut.NewOrderAsync(sessionId, ["example.com"], "http-01");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("tokenPart", result.Value);
        var path = Path.Combine(fx.AppOptions.Value.AcmeFolder, "tokenPart");
        Assert.True(File.Exists(path));
        Assert.Equal("tokenPart.rest.of.token", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public void GetTermsOfService_WhenLetsEncryptFails_Propagates()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.GetTermsOfServiceUri(It.IsAny<Guid>()))
            .Returns(Result<string?>.InternalServerError(null, "no uri"));

        var sut = CreateSut(fx, le);

        var result = sut.GetTermsOfService(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void GetTermsOfService_WhenPdfAlreadyOnDisk_ReturnsBase64WithoutHttp()
    {
        using var fx = new WebApiTestFixture();
        var fileName = "cached-tos.pdf";
        var dataPath = Path.Combine(fx.AppOptions.Value.DataFolder, fileName);
        File.WriteAllBytes(dataPath, [7, 7, 7]);

        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.GetTermsOfServiceUri(It.IsAny<Guid>()))
            .Returns(Result<string?>.Ok($"https://acme.test/sub/{fileName}"));

        var httpHit = false;
        var sut = CreateSut(fx, le, httpHandler: new StubHttpMessageHandler(_ =>
        {
            httpHit = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        var result = sut.GetTermsOfService(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(Convert.ToBase64String([7, 7, 7]), result.Value);
        Assert.False(httpHit);
    }

    [Fact]
    public void AcmeChallenge_WhenFileMissing_ReturnsNotFound()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le);

        var result = sut.AcmeChallenge("missing-challenge");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void AcmeChallenge_WhenFileExists_ReturnsContent()
    {
        using var fx = new WebApiTestFixture();
        var name = "challenge-token";
        File.WriteAllText(Path.Combine(fx.AppOptions.Value.AcmeFolder, name), "challenge-body");
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le);

        var result = sut.AcmeChallenge(name);

        Assert.True(result.IsSuccess);
        Assert.Equal("challenge-body", result.Value);
    }

    [Fact]
    public async Task ApplyCertificatesAsync_WhenAccountDisabled_ReturnsBadRequest()
    {
        using var fx = new WebApiTestFixture();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "d",
            Contacts = [],
            IsStaging = false,
            ChallengeType = "http-01",
            IsDisabled = true,
            CachedCerts = new Dictionary<string, CertificateCache>
            {
                ["h"] = new CertificateCache { Cert = "x", Private = null, PrivatePem = "y" }
            }
        };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le, cache);

        var result = await sut.ApplyCertificatesAsync(accountId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyCertificatesAsync_WhenStaging_ReturnsUnprocessable()
    {
        using var fx = new WebApiTestFixture();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "d",
            Contacts = [],
            IsStaging = true,
            ChallengeType = "http-01",
            IsDisabled = false,
            CachedCerts = new Dictionary<string, CertificateCache>
            {
                ["h"] = new CertificateCache { Cert = "x", Private = null, PrivatePem = "y" }
            }
        };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le, cache);

        var result = await sut.ApplyCertificatesAsync(accountId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyCertificatesAsync_WhenProduction_CallsAgentUploadAndReload()
    {
        using var fx = new WebApiTestFixture();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "d",
            Contacts = [],
            IsStaging = false,
            ChallengeType = "http-01",
            IsDisabled = false,
            CachedCerts = new Dictionary<string, CertificateCache>
            {
                ["h"] = new CertificateCache { Cert = "CERT", Private = null, PrivatePem = "PEM" }
            }
        };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));
        var agent = new Mock<IAgentService>();
        agent.Setup(a => a.UploadCerts(It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Ok());
        agent.Setup(a => a.ReloadService(fx.AppOptions.Value.Agent.ServiceToReload))
            .ReturnsAsync(Result.Ok());
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le, cache, agent);

        var result = await sut.ApplyCertificatesAsync(accountId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        agent.Verify(a => a.UploadCerts(It.IsAny<Dictionary<string, string>>()), Times.Once);
        agent.Verify(a => a.ReloadService(fx.AppOptions.Value.Agent.ServiceToReload), Times.Once);
    }

    [Fact]
    public async Task CompleteChallengesAsync_DelegatesToLetsEncrypt()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.CompleteChallenges(sessionId))
            .ReturnsAsync(Result.Ok());
        var sut = CreateSut(fx, le);

        var result = await sut.CompleteChallengesAsync(sessionId);

        Assert.True(result.IsSuccess);
        le.Verify(x => x.CompleteChallenges(sessionId), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_DelegatesToLetsEncrypt()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.GetOrder(sessionId, It.IsAny<string[]>()))
            .ReturnsAsync(Result.Ok());
        var sut = CreateSut(fx, le);

        var result = await sut.GetOrderAsync(sessionId, ["a.com"]);

        Assert.True(result.IsSuccess);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) => _factory = factory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_factory(request));
    }
}
