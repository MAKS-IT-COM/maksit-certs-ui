using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Models.CertsUI.Account.Requests;
using MaksIT.Results;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

public class AccountServiceTests
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

    private static AccountService CreateSut(WebApiTestFixture fx, ICacheService cache, ICertsFlowService flow) =>
        new(NullLogger<AccountService>.Instance, fx.AppOptions, cache, flow, new AccountToResponseMapper());

    [Fact]
    public async Task GetAccountsAsync_WhenCacheEmpty_ReturnsEmptyArray()
    {
        using var fx = new WebApiTestFixture();

        var cache = new Mock<ICacheService>();
        cache
            .Setup(c => c.LoadAccountsFromCacheAsync())
            .ReturnsAsync(Result<RegistrationCache[]?>.Ok(Array.Empty<RegistrationCache>()));

        var flow = new Mock<ICertsFlowService>();

        var sut = CreateSut(fx, cache.Object, flow.Object);

        var result = await sut.GetAccountsAsync(TestAuth());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
        cache.Verify(c => c.LoadAccountsFromCacheAsync(), Times.Once);
        flow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAccountAsync_WhenPresent_ReturnsMappedResponse()
    {
        using var fx = new WebApiTestFixture();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "acc",
            Contacts = ["mailto:a@b"],
            IsStaging = false,
            ChallengeType = "http-01",
            IsDisabled = false
        };
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .ReturnsAsync(Result<RegistrationCache?>.Ok(reg));
        var flow = new Mock<ICertsFlowService>();

        var sut = CreateSut(fx, cache.Object, flow.Object);

        var result = await sut.GetAccountAsync(TestAuth(), accountId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(accountId, result.Value!.AccountId);
        Assert.Equal("acc", result.Value.Description);
        flow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAccountAsync_calls_DeleteAccountCacheAsync()
    {
        using var fx = new WebApiTestFixture();
        var id = Guid.NewGuid();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.DeleteAccountCacheAsync(id)).ReturnsAsync(Result.Ok());
        var flow = new Mock<ICertsFlowService>();
        var sut = CreateSut(fx, cache.Object, flow.Object);

        var result = await sut.DeleteAccountAsync(TestAuth(), id);

        Assert.True(result.IsSuccess);
        cache.Verify(c => c.DeleteAccountCacheAsync(id), Times.Once);
        flow.VerifyNoOtherCalls();
    }
}
