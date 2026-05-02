using LinqToDB;
using LinqToDB.Data;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

[Collection("postgres-cache")]
public class CacheServiceTests(PostgresCacheFixture pg) {

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

  private CacheService CreateSut() {
    var persistence = new RegistrationCachePersistenceServiceLinq2Db(NullLogger<RegistrationCachePersistenceServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var domain = new RegistrationCacheDomainService(NullLogger<RegistrationCacheDomainService>.Instance, persistence);
    return new CacheService(NullLogger<CacheService>.Instance, pg.Config.AppOptions, domain);
  }

  [Fact]
  public async Task LoadAccountsFromCacheAsync_WhenNoRows_ReturnsEmptyArray() {
    await ClearCachesAsync();
    var sut = CreateSut();

    var result = await sut.LoadAccountsFromCacheAsync();

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Empty(result.Value);
  }

  [Fact]
  public async Task SaveToCacheAsync_LoadAccountFromCacheAsync_DeleteAccountCacheAsync_roundtrip() {
    await ClearCachesAsync();
    var sut = CreateSut();

    var accountId = Guid.NewGuid();
    var cache = new RegistrationCache {
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
  public async Task LoadAccountFromCacheAsync_WhenMissing_ReturnsError() {
    await ClearCachesAsync();
    var sut = CreateSut();

    var result = await sut.LoadAccountFromCacheAsync(Guid.NewGuid());

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task LoadAccountFromCacheAsync_WhenPayloadEmpty_ReturnsError() {
    await ClearCachesAsync();
    var id = Guid.NewGuid();
    using (var db = pg.ConnectionFactory.Create()) {
      db.Insert(new RegistrationCacheDto { Id = id, AccountId = id, PayloadJson = "" });
    }

    var sut = CreateSut();
    var result = await sut.LoadAccountFromCacheAsync(id);

    Assert.False(result.IsSuccess);
  }

  [Fact]
  public async Task DeleteCacheAsync_RemovesAllRegistrationCaches() {
    await ClearCachesAsync();
    var sut = CreateSut();
    var a = Guid.NewGuid();
    var b = Guid.NewGuid();
    await sut.SaveToCacheAsync(a, NewReg(a, "a"));
    await sut.SaveToCacheAsync(b, NewReg(b, "b"));

    var result = await sut.DeleteCacheAsync(TestAuth());

    Assert.True(result.IsSuccess);
    var load = await sut.LoadAccountsFromCacheAsync();
    Assert.True(load.IsSuccess);
    Assert.Empty(load.Value!);
  }

  [Fact]
  public async Task DeleteAccountCacheAsync_WhenRowMissing_still_ok() {
    await ClearCachesAsync();
    var sut = CreateSut();

    var result = await sut.DeleteAccountCacheAsync(Guid.NewGuid());

    Assert.True(result.IsSuccess);
  }

  private Task ClearCachesAsync() {
    using var db = (DataConnection)pg.ConnectionFactory.Create();
    db.Execute("DELETE FROM registration_caches");
    return Task.CompletedTask;
  }

  private static RegistrationCache NewReg(Guid accountId, string description) =>
    new() {
      AccountId = accountId,
      Description = description,
      Contacts = ["mailto:test@example.com"],
      IsStaging = false,
      ChallengeType = "http-01",
      IsDisabled = false
    };
}
