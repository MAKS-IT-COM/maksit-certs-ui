using MaksIT.Models.LetsEncryptServer.ApiKeys;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db;
using MaksIT.CertsUI.Engine.QueryServices.Linq2Db.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Mappers;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Tests.Infrastructure;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

[Collection("postgres-cache")]
public class ApiKeyQueryServiceIntegrationTests(PostgresCacheFixture pg) {

  static JwtTokenData AnyJwt() => new() {
    UserId = Guid.NewGuid(),
    Username = "tester",
    Token = "token",
    IssuedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
  };

  [Fact]
  public async Task Create_then_search_returns_new_key_even_without_description() {
    var queryService = new ApiKeyQueryServiceLinq2Db(NullLogger<ApiKeyQueryServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var apiKeyPersistence = new ApiKeyPersistanceServiceLinq2Db(NullLogger<ApiKeyPersistanceServiceLinq2Db>.Instance, pg.ConnectionFactory);
    var idConfig = new TestIdentityDomainConfiguration("x", "i", "a", 60, 7, "test-pepper-for-api-key-hashing");
    var apiKeyDomainService = new ApiKeyDomainService(NullLogger<ApiKeyDomainService>.Instance, apiKeyPersistence, idConfig);
    var apiKeyService = new ApiKeyService(
      NullLogger<ApiKeyService>.Instance,
      apiKeyDomainService,
      queryService,
      new ApiKeyEntityScopeQueryServiceStub(NullLogger<ApiKeyEntityScopeQueryServiceStub>.Instance),
      new ApiKeyToResponseMapper()
    );

    var created = await apiKeyService.CreateAPIKeyAsync(AnyJwt(), new CreateApiKeyRequest {
      Description = null
    });
    Assert.True(created.IsSuccess);
    Assert.NotNull(created.Value);
    Assert.False(string.IsNullOrWhiteSpace(created.Value!.ApiKey));
    Assert.Contains('|', created.Value.ApiKey);

    var search = queryService.Search(apiKeysPredicate: null, skip: 0, limit: 50);

    Assert.True(search.IsSuccess);
    Assert.NotNull(search.Value);
    Assert.Contains(search.Value!, x => x.Id == created.Value!.Id);
  }
}
