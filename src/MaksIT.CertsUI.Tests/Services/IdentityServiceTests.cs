using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

public class IdentityServiceTests {
  private static TestIdentityDomainConfiguration BuildIdentityConfig(WebApiTestFixture fx) {
    var jwt = fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration;
    return new(
      jwt.JwtSecret,
      jwt.Issuer,
      jwt.Audience,
      jwt.ExpiresIn,
      jwt.RefreshTokenExpiresIn,
      jwt.PasswordPepper
    );
  }

  [Fact]
  public async Task LoginAsync_WhenUserMissing_ReturnsNotFound() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);

    var result = await sut.LoginAsync(new LoginRequest { Username = "nobody", Password = "x" });

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task LoginAsync_WithValidAdminCredentials_ReturnsTokens() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    await store.EnsureDefaultAdminAsync(
      fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Username,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Password);
    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);

    var result = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.False(string.IsNullOrEmpty(result.Value.Token));
    Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
  }

  [Fact]
  public async Task RefreshTokenAsync_WhenTokenInvalid_ReturnsUnauthorized() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    await store.EnsureDefaultAdminAsync(
      fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Username,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Password);
    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);

    var result = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "not-a-real-token" });

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task RefreshTokenAsync_WhenTokenValid_ReturnsSameAccessUntilExpiry() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    await store.EnsureDefaultAdminAsync(
      fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Username,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Password);
    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);
    var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(login.IsSuccess);

    var refresh = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Value!.RefreshToken });

    Assert.True(refresh.IsSuccess);
    Assert.Equal(login.Value.Token, refresh.Value!.Token);
  }

  [Fact]
  public async Task Logout_removes_matching_access_token() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    await store.EnsureDefaultAdminAsync(
      fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Username,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Password);
    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);
    var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(login.IsSuccess);
    var adminUser = (await store.GetByNameAsync("admin")).Value!;

    var jwt = new JwtTokenData {
      UserId = adminUser.Id,
      Username = "admin",
      Token = login.Value!.Token,
      IssuedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddMinutes(5)
    };
    await sut.Logout(jwt, new LogoutRequest { Token = login.Value.Token, LogoutFromAllDevices = false });

    var second = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(second.IsSuccess);
    Assert.NotEqual(login.Value.Token, second.Value!.Token);
  }

  [Fact]
  public async Task PatchUserAsync_SetPassword_allows_login_with_new_password() {
    using var fx = new WebApiTestFixture();
    var store = new InMemoryUserStore();
    await store.EnsureDefaultAdminAsync(
      fx.AppOptions.Value.CertsUIEngineConfiguration.JwtSettingsConfiguration.PasswordPepper,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Username,
      fx.AppOptions.Value.CertsUIEngineConfiguration.Admin.Password);
    var admin = (await store.GetByNameAsync("admin")).Value!;

    var idCfg = BuildIdentityConfig(fx);
    var domainService = new IdentityDomainService(NullLogger<IdentityDomainService>.Instance, store, idCfg, idCfg, idCfg);
    var sut = new IdentityService(NullLogger<IdentityService>.Instance, fx.AppOptions, domainService, (IUserQueryService)store, new UserToResponseMapper(), idCfg);
    var jwt = new JwtTokenData {
      UserId = admin.Id,
      Username = admin.Username,
      Token = "unused-for-patch",
      IssuedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddMinutes(5)
    };
    var patch = new PatchUserRequest {
      Password = "new-secret",
      Operations = new Dictionary<string, PatchOperation> {
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
