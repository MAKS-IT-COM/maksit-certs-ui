using LinqToDB.Data;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Persistence.Mappers;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;
using MaksIT.CertsUI.Models.Identity.Login;
using MaksIT.CertsUI.Models.Identity.Logout;
using MaksIT.CertsUI.Models.Identity.User;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

/// <summary>Identity host service integration tests against PostgreSQL + Linq2DB.</summary>
[Collection("postgres-cache")]
public class IdentityServiceTests(PostgresCacheFixture pg) {

  private ICertsEngineConfiguration EngineCfg => pg.Config.AppOptions.Value.CertsEngineConfiguration;

  private IdentityDomainService CreateIdentityDomainService() {
    var engineCfg = EngineCfg;
    var userMapper = new UserMapper(engineCfg.JwtSettingsConfiguration.PasswordPepper);
    var persistence = new IdentityPersistenceServiceLinq2Db(
      NullLogger<IdentityPersistenceServiceLinq2Db>.Instance,
      pg.ConnectionFactory,
      userMapper);
    var userAuthz = new UserAuthorizationPersistenceServiceLinq2Db(
      NullLogger<UserAuthorizationPersistenceServiceLinq2Db>.Instance,
      pg.ConnectionFactory);
    return new IdentityDomainService(
      NullLogger<IdentityDomainService>.Instance,
      persistence,
      userAuthz,
      engineCfg,
      engineCfg.Admin,
      engineCfg.JwtSettingsConfiguration,
      engineCfg.TwoFactorSettingsConfiguration);
  }

  private IdentityService CreateSut() {
    var domain = CreateIdentityDomainService();
    var query = new IdentityQueryServiceLinq2Db(NullLogger<IdentityQueryServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var scopeQuery = new UserEntityScopeQueryServiceLinq2Db(
      NullLogger<UserEntityScopeQueryServiceLinq2Db>.Instance,
      pg.ConnectionFactory);
    return new IdentityService(
      NullLogger<IdentityService>.Instance,
      pg.Config.AppOptions,
      query,
      scopeQuery,
      domain,
      new UserToResponseMapper());
  }

  private void ClearIdentityTables() {
    using var db = (DataConnection)pg.ConnectionFactory.Create();
    db.Execute("DELETE FROM jwt_tokens");
    db.Execute("DELETE FROM two_factor_recovery_codes");
    db.Execute("DELETE FROM users");
  }

  [Fact]
  public async Task LoginAsync_WhenUserMissing_ReturnsNotFound() {
    ClearIdentityTables();
    var sut = CreateSut();

    var result = await sut.LoginAsync(new LoginRequest { Username = "nobody", Password = "x" });

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task LoginAsync_WithValidAdminCredentials_ReturnsTokens() {
    ClearIdentityTables();
    var domain = CreateIdentityDomainService();
    var seeded = await domain.InitializeAdminAsync();
    Assert.True(seeded.IsSuccess);

    var sut = CreateSut();

    var result = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.False(string.IsNullOrEmpty(result.Value.Token));
    Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
  }

  [Fact]
  public async Task RefreshTokenAsync_WhenTokenInvalid_ReturnsUnauthorized() {
    ClearIdentityTables();
    var domain = CreateIdentityDomainService();
    Assert.True((await domain.InitializeAdminAsync()).IsSuccess);

    var sut = CreateSut();

    var result = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "not-a-real-token" });

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task RefreshTokenAsync_WhenTokenValid_ReturnsSameAccessUntilExpiry() {
    ClearIdentityTables();
    var domain = CreateIdentityDomainService();
    Assert.True((await domain.InitializeAdminAsync()).IsSuccess);

    var sut = CreateSut();
    var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(login.IsSuccess);

    var refresh = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Value!.RefreshToken });

    Assert.True(refresh.IsSuccess);
    Assert.Equal(login.Value.Token, refresh.Value!.Token);
  }

  [Fact]
  public async Task Logout_removes_matching_access_token() {
    ClearIdentityTables();
    var seedDomain = CreateIdentityDomainService();
    Assert.True((await seedDomain.InitializeAdminAsync()).IsSuccess);

    var sut = CreateSut();
    var login = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(login.IsSuccess);
    var adminRead = seedDomain.ReadUserByUsername("admin");
    Assert.True(adminRead.IsSuccess);
    var adminUser = adminRead.Value!;

    var jwt = new JwtTokenData {
      UserId = adminUser.Id,
      Username = "admin",
      Token = login.Value!.Token,
      ClaimRoles = [],
      IssuedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.AddMinutes(5)
    };
    await sut.Logout(jwt, new LogoutRequest { LogoutFromAllDevices = false });

    var second = await sut.LoginAsync(new LoginRequest { Username = "admin", Password = "password" });
    Assert.True(second.IsSuccess);
    Assert.NotEqual(login.Value.Token, second.Value!.Token);
  }

  [Fact]
  public async Task PatchUserAsync_SetPassword_allows_login_with_new_password() {
    ClearIdentityTables();
    var seedDomain = CreateIdentityDomainService();
    Assert.True((await seedDomain.InitializeAdminAsync()).IsSuccess);
    var adminRead = seedDomain.ReadUserByUsername("admin");
    Assert.True(adminRead.IsSuccess);
    var admin = adminRead.Value!;

    var sut = CreateSut();
    var jwt = new JwtTokenData {
      UserId = admin.Id,
      Username = admin.Username,
      Token = "unused-for-patch",
      ClaimRoles = [],
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
