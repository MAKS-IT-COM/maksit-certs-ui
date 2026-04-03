using MaksIT.Core.Webapi.Models;
using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;
using MaksIT.Webapi.Authorization;
using MaksIT.Webapi.Domain;
using MaksIT.Webapi.Services;
using MaksIT.Webapi.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.Webapi.Tests.Services;

public class IdentityServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenUserMissing_ReturnsNotFound()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        await settingsService.SaveAsync(new Settings());

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);

        var result = await sut.LoginAsync(new LoginRequest { Username = "nobody", Password = "x" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_WithValidAdminCredentials_ReturnsTokens()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        Assert.True(init.IsSuccess);
        await settingsService.SaveAsync(init.Value!);

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);

        var result = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Value.Token));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenInvalid_ReturnsUnauthorized()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        await settingsService.SaveAsync(init.Value!);

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);

        var result = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "not-a-real-token" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenValid_ReturnsSameAccessUntilExpiry()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        await settingsService.SaveAsync(init.Value!);

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);
        var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
        Assert.True(login.IsSuccess);

        var refresh = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Value!.RefreshToken });

        Assert.True(refresh.IsSuccess);
        Assert.Equal(login.Value.Token, refresh.Value!.Token);
    }

    [Fact]
    public async Task Logout_removes_matching_access_token()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        await settingsService.SaveAsync(init.Value!);

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);
        var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
        Assert.True(login.IsSuccess);

        await sut.Logout(new LogoutRequest { Token = login.Value!.Token, LogoutFromAllDevices = false });

        var second = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
        Assert.True(second.IsSuccess);
        Assert.NotEqual(login.Value.Token, second.Value!.Token);
    }

    [Fact]
    public async Task PatchUserAsync_SetPassword_allows_login_with_new_password()
    {
        using var fx = new WebApiTestFixture();
        var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, fx.AppOptions);
        var init = new Settings().Initialize(fx.AppOptions.Value.Auth.Pepper);
        await settingsService.SaveAsync(init.Value!);
        var admin = init.Value!.Users[0];

        var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, settingsService);
        var jwt = new JwtTokenData
        {
            UserId = admin.Id,
            Username = admin.Name,
            Token = "unused-for-patch",
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        var patch = new PatchUserRequest
        {
            Password = "new-secret",
            Operations = new Dictionary<string, PatchOperation>
            {
                [nameof(PatchUserRequest.Password)] = PatchOperation.SetField
            }
        };

        var patched = await sut.PatchUserAsync(jwt, admin.Id, patch);
        Assert.True(patched.IsSuccess);

        var oldLogin = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
        Assert.False(oldLogin.IsSuccess);

        var newLogin = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "new-secret" });
        Assert.True(newLogin.IsSuccess);
    }
}
