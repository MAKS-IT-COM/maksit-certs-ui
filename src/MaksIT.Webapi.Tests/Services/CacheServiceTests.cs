using MaksIT.LetsEncrypt.Entities;
using MaksIT.Webapi.Services;
using MaksIT.Webapi.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.Webapi.Tests.Services;

public class CacheServiceTests
{
    [Fact]
    public async Task LoadAccountsFromCacheAsync_WhenNoJsonFiles_ReturnsEmptyArray()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);

        var result = await sut.LoadAccountsFromCacheAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SaveToCacheAsync_LoadAccountFromCacheAsync_DeleteAccountCacheAsync_roundtrip()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);

        var accountId = Guid.NewGuid();
        var cache = new RegistrationCache
        {
            AccountId = accountId,
            Description = "unit-test",
            Contacts = ["mailto:test@example.com"],
            IsStaging = true,
            ChallengeType = "http-01",
            IsDisabled = false
        };

        var save = await sut.SaveToCacheAsync(accountId, cache);
        Assert.True(save.IsSuccess);

        var load = await sut.LoadAccountFromCacheAsync(accountId);
        Assert.True(load.IsSuccess);
        Assert.NotNull(load.Value);
        Assert.Equal(accountId, load.Value.AccountId);
        Assert.Equal("unit-test", load.Value.Description);

        var del = await sut.DeleteAccountCacheAsync(accountId);
        Assert.True(del.IsSuccess);

        var loadAfter = await sut.LoadAccountFromCacheAsync(accountId);
        Assert.False(loadAfter.IsSuccess);
    }

    [Fact]
    public async Task LoadAccountFromCacheAsync_WhenFileMissing_ReturnsError()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);

        var result = await sut.LoadAccountFromCacheAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoadAccountFromCacheAsync_WhenFileEmpty_ReturnsError()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(Path.Combine(fx.CacheFolderPath, $"{id}.json"), "");

        var result = await sut.LoadAccountFromCacheAsync(id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteCacheAsync_RemovesFilesInCacheFolder()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);
        await File.WriteAllTextAsync(Path.Combine(fx.CacheFolderPath, "extra.txt"), "x");

        var result = await sut.DeleteCacheAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(Directory.GetFiles(fx.CacheFolderPath));
    }

    [Fact]
    public async Task DeleteAccountCacheAsync_WhenFileMissing_still_ok()
    {
        using var fx = new WebApiTestFixture();
        using var sut = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions);

        var result = await sut.DeleteAccountCacheAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
    }
}
