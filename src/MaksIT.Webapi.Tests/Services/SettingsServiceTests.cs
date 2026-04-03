using MaksIT.Webapi.Domain;
using MaksIT.Webapi.Services;
using MaksIT.Webapi.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.Webapi.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenFileMissing_ReturnsEmptySettings()
    {
        using var fx = new WebApiTestFixture();
        var sut = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);

        var result = await sut.LoadAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.Init);
        Assert.Empty(result.Value.Users);
    }

    [Fact]
    public async Task SaveAsync_and_LoadAsync_roundtrip_preserves_users()
    {
        using var fx = new WebApiTestFixture();
        var sut = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);

        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        Assert.True(init.IsSuccess);
        var save = await sut.SaveAsync(init.Value!);
        Assert.True(save.IsSuccess);

        var loaded = await sut.LoadAsync();
        Assert.True(loaded.IsSuccess);
        Assert.NotNull(loaded.Value);
        Assert.True(loaded.Value.Init);
        Assert.Single(loaded.Value.Users);
        Assert.Equal("admin", loaded.Value.Users[0].Name);
    }
}
