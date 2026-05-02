using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Models.CertsUI.Account.Requests;
using MaksIT.Results;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using LinqToDB.Data;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;
using MaksIT.CertsUI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

[Collection("postgres-cache")]
public class AccountServicePatchAccountIntegrationTests(PostgresCacheFixture pg) {

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
  public async Task PatchAccountAsync_SetDescription_persists_and_returns_updated() {
    using (var db = (DataConnection)pg.ConnectionFactory.Create())
      db.Execute("DELETE FROM registration_caches");

    using var fx = pg.Config;
    var accountId = Guid.NewGuid();
    var reg = new RegistrationCache {
      AccountId = accountId,
      Description = "old",
      Contacts = ["mailto:a@b"],
      IsStaging = false,
      ChallengeType = "http-01",
      IsDisabled = false
    };
    var cachePersistence = new RegistrationCachePersistenceServiceLinq2Db(NullLogger<RegistrationCachePersistenceServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var cacheDomain = new RegistrationCacheDomainService(NullLogger<RegistrationCacheDomainService>.Instance, cachePersistence);
    var cacheSvc = new CacheService(NullLogger<CacheService>.Instance, fx.AppOptions, cacheDomain);
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

    var patch = new PatchAccountRequest {
      Description = "new-desc",
      Operations = new Dictionary<string, PatchOperation> {
        [nameof(PatchAccountRequest.Description)] = PatchOperation.SetField
      }
    };

    var result = await sut.PatchAccountAsync(TestAuth(), accountId, patch);

    Assert.True(result.IsSuccess);
    Assert.Equal("new-desc", result.Value!.Description);
    var reload = await cacheSvc.LoadAccountFromCacheAsync(accountId);
    Assert.True(reload.IsSuccess);
    Assert.Equal("new-desc", reload.Value!.Description);
    flow.VerifyNoOtherCalls();
  }
}
