using System.Net;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.Engine.Services;
using MaksIT.Results;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

public sealed class CertsFlowServiceTests
{
    private sealed class TestCertsFlowEngineConfiguration(WebApiTestFixture fx) : ICertsFlowEngineConfiguration {
        public string AcmeFolder => fx.AppOptions.Value.CertsUIEngineConfiguration.AcmeFolder;
        public string DataFolder => fx.AppOptions.Value.CertsUIEngineConfiguration.DataFolder;
        public string AgentServiceToReload => fx.AppOptions.Value.CertsUIEngineConfiguration.Agent.ServiceToReload;
    }

    private static CertsFlowDomainService CreateSut(
        WebApiTestFixture fx,
        Mock<ILetsEncryptService> le,
        Mock<IRegistrationCachePersistanceService>? registrationCache = null,
        Mock<IAgentDeploymentService>? agent = null,
        Mock<ITermsOfServiceCachePersistenceService>? termsOfServiceCache = null,
        Mock<IAcmeHttpChallengePersistenceService>? httpChallenges = null,
        Mock<IRuntimeLeaseService>? runtimeLease = null,
        Mock<IRuntimeInstanceId>? runtimeInstance = null,
        HttpMessageHandler? httpHandler = null,
        Mock<IPrimaryReplicaWorkload>? primaryReplica = null)
    {
        registrationCache ??= new Mock<IRegistrationCachePersistanceService>();
        agent ??= new Mock<IAgentDeploymentService>();
        var tosCacheProvided = termsOfServiceCache is not null;
        termsOfServiceCache ??= new Mock<ITermsOfServiceCachePersistenceService>();
        if (!tosCacheProvided) {
            termsOfServiceCache.Setup(c => c.GetByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TermsOfServiceCacheDto?>.NotFound(null, "missing"));
            termsOfServiceCache.Setup(c => c.UpsertAsync(It.IsAny<TermsOfServiceCacheDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());
        }
        var httpChallengesProvided = httpChallenges is not null;
        httpChallenges ??= new Mock<IAcmeHttpChallengePersistenceService>();
        if (!httpChallengesProvided) {
            httpChallenges.Setup(c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());
            httpChallenges.Setup(c => c.GetTokenValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string?>.NotFound(null, "missing"));
            httpChallenges.Setup(c => c.DeleteOlderThanAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Ok(0));
        }
        var runtimeLeaseProvided = runtimeLease is not null;
        runtimeLease ??= new Mock<IRuntimeLeaseService>();
        if (!runtimeLeaseProvided) {
            runtimeLease.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Ok(true));
            runtimeLease.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());
        }
        var runtimeInstanceProvided = runtimeInstance is not null;
        runtimeInstance ??= new Mock<IRuntimeInstanceId>();
        if (!runtimeInstanceProvided)
            runtimeInstance.Setup(i => i.InstanceId).Returns("test-instance");
        var primaryWorkload = primaryReplica ?? new Mock<IPrimaryReplicaWorkload>();
        if (primaryReplica is null)
            primaryWorkload.Setup(p => p.IsPrimary).Returns(true);
        var handler = httpHandler ?? new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]) });
        var httpClient = new HttpClient(handler, disposeHandler: true);
        return new CertsFlowDomainService(
            NullLogger<CertsFlowDomainService>.Instance,
            httpClient,
            le.Object,
            registrationCache.Object,
            agent.Object,
            new TestCertsFlowEngineConfiguration(fx),
            termsOfServiceCache.Object,
            httpChallenges.Object,
            runtimeLease.Object,
            runtimeInstance.Object,
            primaryWorkload.Object);
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
    public async Task ConfigureClientAsync_WhenNotPrimary_ReturnsServiceUnavailableWithMarker()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        var primary = new Mock<IPrimaryReplicaWorkload>();
        primary.Setup(p => p.IsPrimary).Returns(false);
        var sut = CreateSut(fx, le, primaryReplica: primary);

        var result = await sut.ConfigureClientAsync(isStaging: false);

        Assert.False(result.IsSuccess);
        Assert.Contains(CertsFlowPrimaryReplica.DiagnosticMarker, result.Messages ?? []);
        le.Verify(x => x.ConfigureClient(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task AcmeChallenge_WhenNotPrimary_StillSucceedsFromDatabase()
    {
        using var fx = new WebApiTestFixture();
        var name = "challenge-token";
        var le = new Mock<ILetsEncryptService>();
        var primary = new Mock<IPrimaryReplicaWorkload>();
        primary.Setup(p => p.IsPrimary).Returns(false);
        var challenges = new Mock<IAcmeHttpChallengePersistenceService>();
        challenges.Setup(c => c.GetTokenValueAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.Ok("body"));
        challenges.Setup(c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        challenges.Setup(c => c.DeleteOlderThanAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(0));
        var sut = CreateSut(fx, le, httpChallenges: challenges, primaryReplica: primary);

        var result = await sut.AcmeChallengeAsync(name, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("body", result.Value);
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
        var cache = new Mock<IRegistrationCachePersistanceService>();
        cache.Setup(c => c.LoadAsync(requestedAccount, It.IsAny<CancellationToken>()))
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
        var cache = new Mock<IRegistrationCachePersistanceService>();
        cache.Setup(c => c.LoadAsync(accountId, It.IsAny<CancellationToken>()))
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
    public async Task NewOrderAsync_WhenOrderSucceeds_PersistsChallengesInDatabase()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.NewOrder(sessionId, It.IsAny<string[]>(), "http-01"))
            .ReturnsAsync(Result<Dictionary<string, string>?>.Ok(new Dictionary<string, string>
            {
                ["example.com"] = "tokenPart.rest.of.token"
            }));

        var challenges = new Mock<IAcmeHttpChallengePersistenceService>();
        challenges.Setup(c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        challenges.Setup(c => c.GetTokenValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.NotFound(null, "missing"));
        challenges.Setup(c => c.DeleteOlderThanAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(0));
        var sut = CreateSut(fx, le, httpChallenges: challenges);

        var result = await sut.NewOrderAsync(sessionId, ["example.com"], "http-01");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("tokenPart", result.Value);
        challenges.Verify(c => c.UpsertAsync("tokenPart", "tokenPart.rest.of.token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NewOrderAsync_WhenLeaseBusy_DoesNotStartAcmeOrder()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        var runtimeLease = new Mock<IRuntimeLeaseService>();
        runtimeLease.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(false));
        runtimeLease.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le, runtimeLease: runtimeLease);

        var result = await sut.NewOrderAsync(sessionId, ["example.com"], "http-01");

        Assert.False(result.IsSuccess);
        le.Verify(x => x.NewOrder(It.IsAny<Guid>(), It.IsAny<string[]>(), It.IsAny<string>()), Times.Never);
        runtimeLease.Verify(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NewOrderAsync_WhenLeaseAcquireFails_ReturnsFailureWithoutOrderCall()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        var runtimeLease = new Mock<IRuntimeLeaseService>();
        runtimeLease.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.InternalServerError(false, "lease error"));
        runtimeLease.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le, runtimeLease: runtimeLease);

        var result = await sut.NewOrderAsync(sessionId, ["example.com"], "http-01");

        Assert.False(result.IsSuccess);
        le.Verify(x => x.NewOrder(It.IsAny<Guid>(), It.IsAny<string[]>(), It.IsAny<string>()), Times.Never);
        runtimeLease.Verify(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NewOrderAsync_WhenOrderFails_StillReleasesLease()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.NewOrder(sessionId, It.IsAny<string[]>(), "http-01"))
            .ReturnsAsync(Result<Dictionary<string, string>?>.InternalServerError(null, "acme failed"));
        var runtimeLease = new Mock<IRuntimeLeaseService>();
        runtimeLease.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));
        runtimeLease.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var sut = CreateSut(fx, le, runtimeLease: runtimeLease);

        var result = await sut.NewOrderAsync(sessionId, ["example.com"], "http-01");

        Assert.False(result.IsSuccess);
        runtimeLease.Verify(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTermsOfServiceAsync_WhenLetsEncryptFails_Propagates()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.GetTermsOfServiceUri(sessionId))
            .Returns(Result<string?>.InternalServerError(null, "no uri"));

        var sut = CreateSut(fx, le);

        var result = await sut.GetTermsOfServiceAsync(sessionId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetTermsOfServiceAsync_WhenCachedAndNotExpired_ReturnsBase64WithoutHttp()
    {
        using var fx = new WebApiTestFixture();
        var sessionId = Guid.NewGuid();
        var url = "https://acme.test/sub/cached-tos.pdf";
        var le = new Mock<ILetsEncryptService>();
        le.Setup(x => x.GetTermsOfServiceUri(sessionId))
            .Returns(Result<string?>.Ok(url));

        var tosCache = new Mock<ITermsOfServiceCachePersistenceService>();
        tosCache.Setup(c => c.GetByUrlAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TermsOfServiceCacheDto?>.Ok(new TermsOfServiceCacheDto {
                Url = url,
                UrlHashHex = "abc",
                ContentType = "application/pdf",
                ContentBytes = [7, 7, 7],
                FetchedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
            }));
        tosCache.Setup(c => c.UpsertAsync(It.IsAny<TermsOfServiceCacheDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        var httpHit = false;
        var sut = CreateSut(fx, le, termsOfServiceCache: tosCache, httpHandler: new StubHttpMessageHandler(_ =>
        {
            httpHit = true;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) };
        }));

        var result = await sut.GetTermsOfServiceAsync(sessionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(Convert.ToBase64String([7, 7, 7]), result.Value);
        Assert.False(httpHit);
    }

    [Fact]
    public async Task AcmeChallenge_WhenMissing_ReturnsNotFound()
    {
        using var fx = new WebApiTestFixture();
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le);

        var result = await sut.AcmeChallengeAsync("missing-challenge", CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AcmeChallenge_WhenDbRowExists_MaterializesFileAndReturnsContent()
    {
        using var fx = new WebApiTestFixture();
        var name = "challenge-token";
        var le = new Mock<ILetsEncryptService>();
        var challenges = new Mock<IAcmeHttpChallengePersistenceService>();
        challenges.Setup(c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        challenges.Setup(c => c.GetTokenValueAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string?>.Ok("challenge-body"));
        challenges.Setup(c => c.DeleteOlderThanAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(0));
        var sut = CreateSut(fx, le, httpChallenges: challenges);

        var result = await sut.AcmeChallengeAsync(name, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("challenge-body", result.Value);
        var path = Path.Combine(fx.AppOptions.Value.CertsUIEngineConfiguration.AcmeFolder, name);
        Assert.True(File.Exists(path));
        Assert.Equal("challenge-body", await File.ReadAllTextAsync(path));
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
        var cache = new Mock<IRegistrationCachePersistanceService>();
        cache.Setup(c => c.LoadAsync(accountId, It.IsAny<CancellationToken>()))
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
        var cache = new Mock<IRegistrationCachePersistanceService>();
        cache.Setup(c => c.LoadAsync(accountId, It.IsAny<CancellationToken>()))
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
        var cache = new Mock<IRegistrationCachePersistanceService>();
        cache.Setup(c => c.LoadAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));
        var agent = new Mock<IAgentDeploymentService>();
        agent.Setup(a => a.UploadCerts(It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Ok());
        agent.Setup(a => a.ReloadService(fx.AppOptions.Value.CertsUIEngineConfiguration.Agent.ServiceToReload))
            .ReturnsAsync(Result.Ok());
        var le = new Mock<ILetsEncryptService>();
        var sut = CreateSut(fx, le, cache, agent);

        var result = await sut.ApplyCertificatesAsync(accountId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        agent.Verify(a => a.UploadCerts(It.IsAny<Dictionary<string, string>>()), Times.Once);
        agent.Verify(a => a.ReloadService(fx.AppOptions.Value.CertsUIEngineConfiguration.Agent.ServiceToReload), Times.Once);
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
