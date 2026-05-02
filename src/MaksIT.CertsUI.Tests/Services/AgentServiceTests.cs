using System.Net;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

public class AgentServiceTests
{
    private static JwtTokenData TestJwt() =>
        new() {
            Token = "test",
            Username = "test",
            ClaimRoles = [],
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserId = Guid.Empty,
            IsGlobalAdmin = true
        };

    private static CertsUIAuthorizationData TestAuth() =>
        new() { JwtTokenData = TestJwt() };

    private sealed class OkHandler : HttpMessageHandler
    {
        private readonly string _body;

        public OkHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("HelloWorld", request.RequestUri!.ToString(), StringComparison.Ordinal);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body)
            });
        }
    }

    [Fact]
    public async Task GetHelloWorld_OnSuccess_ReturnsMessageFromBody()
    {
        using var fx = new WebApiTestFixture();
        var client = new HttpClient(new OkHandler("hello-from-agent"))
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var sut = new AgentService(NullLogger<AgentService>.Instance, fx.AppOptions, client);

        var result = await sut.GetHelloWorld(TestAuth());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("hello-from-agent", result.Value.Message);
    }
}
