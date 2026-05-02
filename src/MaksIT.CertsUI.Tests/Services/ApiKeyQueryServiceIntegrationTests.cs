using MaksIT.CertsUI.Models.APIKeys;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Persistence.Mappers;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;
using MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using MaksIT.CertsUI.Trng;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

[Collection("postgres-cache")]
public class ApiKeyQueryServiceIntegrationTests(PostgresCacheFixture pg) {

  static JwtTokenData AnyJwt() => new() {
    UserId = Guid.NewGuid(),
    Username = "tester",
    Token = "token",
    ClaimRoles = [],
    IssuedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
  };

  [Fact]
  public async Task Create_then_search_returns_new_key_even_without_description() {
    var queryService = new ApiKeyQueryServiceLinq2Db(NullLogger<ApiKeyQueryServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var entityScopeQuery = new ApiKeyEntityScopeQueryServiceLinq2Db(
      NullLogger<ApiKeyEntityScopeQueryServiceLinq2Db>.Instance,
      pg.ConnectionFactory);
    var apiKeyPersistence = new ApiKeyPersistenceServiceLinq2Db(NullLogger<ApiKeyPersistenceServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var apiKeyAuthzPersistence = new ApiKeyAuthorizationPersistenceServiceLinq2Db(
      NullLogger<ApiKeyAuthorizationPersistenceServiceLinq2Db>.Instance,
      pg.ConnectionFactory);
    var apiKeyDomainService = new ApiKeyDomainService(
      NullLogger<ApiKeyDomainService>.Instance,
      apiKeyPersistence,
      apiKeyAuthzPersistence);

    var engineCfg = pg.Config.AppOptions.Value.CertsEngineConfiguration;
    var userMapper = new UserMapper(engineCfg.JwtSettingsConfiguration.PasswordPepper);
    var identityPersistence = new IdentityPersistenceServiceLinq2Db(
      NullLogger<IdentityPersistenceServiceLinq2Db>.Instance,
      pg.ConnectionFactory,
      userMapper);
    var userAuthzPersistence = new UserAuthorizationPersistenceServiceLinq2Db(
      NullLogger<UserAuthorizationPersistenceServiceLinq2Db>.Instance,
      pg.ConnectionFactory);
    var identityDomainService = new IdentityDomainService(
      NullLogger<IdentityDomainService>.Instance,
      identityPersistence,
      userAuthzPersistence,
      engineCfg,
      engineCfg.Admin,
      engineCfg.JwtSettingsConfiguration,
      engineCfg.TwoFactorSettingsConfiguration);

    var apiKeyService = new ApiKeyService(
      NullLogger<ApiKeyService>.Instance,
      pg.Config.AppOptions,
      identityDomainService,
      queryService,
      entityScopeQuery,
      apiKeyDomainService,
      new LocalTrngClient(),
      new ApiKeyToResponseMapper());

    var created = await apiKeyService.CreateAPIKeyAsync(AnyJwt(), new CreateApiKeyRequest {
      Description = null
    });
    Assert.True(created.IsSuccess);
    Assert.NotNull(created.Value);
    Assert.False(string.IsNullOrWhiteSpace(created.Value!.ApiKey));
    Assert.True(created.Value.ApiKey.Length >= 16);

    var search = queryService.Search(apiKeysPredicate: null, skip: 0, limit: 50);

    Assert.True(search.IsSuccess);
    Assert.NotNull(search.Value);
    Assert.Contains(search.Value!, x => x.Id == created.Value!.Id);
  }
}
