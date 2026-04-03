using MaksIT.Core.Webapi.Models;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models.LetsEncryptServer.Account.Requests;
using MaksIT.Results;
using MaksIT.Webapi.Services;
using MaksIT.Webapi.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MaksIT.Webapi.Tests.Services;

public class AccountServiceTests
{
    private static AccountService CreateSut(WebApiTestFixture fx, ICacheService cache, ICertsFlowService flow) =>
        new(NullLogger<CacheService>.Instance, fx.AppOptions, cache, flow);

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

        var result = await sut.GetAccountsAsync();

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

        var result = await sut.GetAccountAsync(accountId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(accountId, result.Value!.AccountId);
        Assert.Equal("acc", result.Value.Description);
        flow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchAccountAsync_SetDescription_persists_and_returns_updated()
    {
        using var fx = new WebApiTestFixture();
        var accountId = Guid.NewGuid();
        var reg = new RegistrationCache
        {
            AccountId = accountId,
            Description = "old",
            Contacts = ["mailto:a@b"],
            IsStaging = false,
            ChallengeType = "http-01",
            IsDisabled = false
        };
        using var cacheSvc = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);
        await cacheSvc.SaveToCacheAsync(accountId, reg);

        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.LoadAccountFromCacheAsync(accountId))
            .Returns(() => cacheSvc.LoadAccountFromCacheAsync(accountId));
        cacheMock
            .Setup(c => c.SaveToCacheAsync(accountId, It.IsAny<RegistrationCache>()))
            .Returns<Guid, RegistrationCache>((_, c) => cacheSvc.SaveToCacheAsync(accountId, c));

        var flow = new Mock<ICertsFlowService>();
        var sut = CreateSut(fx, cacheMock.Object, flow.Object);

        var patch = new PatchAccountRequest
        {
            Description = "new-desc",
            Operations = new Dictionary<string, PatchOperation>
            {
                [nameof(PatchAccountRequest.Description)] = PatchOperation.SetField
            }
        };

        var result = await sut.PatchAccountAsync(accountId, patch);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-desc", result.Value!.Description);
        var reload = await cacheSvc.LoadAccountFromCacheAsync(accountId);
        Assert.True(reload.IsSuccess);
        Assert.Equal("new-desc", reload.Value!.Description);
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

        var result = await sut.DeleteAccountAsync(id);

        Assert.True(result.IsSuccess);
        cache.Verify(c => c.DeleteAccountCacheAsync(id), Times.Once);
        flow.VerifyNoOtherCalls();
    }
}
