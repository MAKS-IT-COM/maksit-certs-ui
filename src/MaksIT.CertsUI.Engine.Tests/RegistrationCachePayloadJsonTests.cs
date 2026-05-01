using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Certs;
using Xunit;

namespace MaksIT.CertsUI.Engine.Tests;

/// <summary>
/// Same JSON contract as <c>registration_caches.PayloadJson</c> (load/save in
/// <see cref="MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db.RegistrationCachePersistanceServiceLinq2Db"/>)
/// and as written into zip entries via <c>RegistrationCache.ToJson()</c> (MaksIT.Core STJ helpers).
/// </summary>
public class RegistrationCachePayloadJsonTests {
  private static readonly Guid Aggregate = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [Fact]
  public void PayloadJson_ToJson_ToObject_roundtrips_like_registration_caches_row() {
    const string acmeResource = "https://acme.example/acct/1";
    var cache = new RegistrationCache {
      AccountId = Aggregate,
      AcmeAccountResourceId = acmeResource,
      Description = "unit payload",
      Contacts = ["mailto:ops@example.com"],
      IsStaging = true,
      ChallengeType = "http-01",
      IsDisabled = false
    };

    var payloadJson = cache.ToJson();
    var loaded = payloadJson.ToObject<RegistrationCache>();
    Assert.NotNull(loaded);

    Assert.Equal(Aggregate, loaded!.AccountId);
    Assert.Equal(acmeResource, loaded.AcmeAccountResourceId);
    Assert.Equal(cache.Description, loaded.Description);
    Assert.Equal(cache.Contacts, loaded.Contacts);
    Assert.Equal(cache.IsStaging, loaded.IsStaging);
    Assert.Equal(cache.ChallengeType, loaded.ChallengeType);
    Assert.Equal(cache.IsDisabled, loaded.IsDisabled);

    var payloadJson2 = loaded.ToJson();
    var loaded2 = payloadJson2.ToObject<RegistrationCache>();
    Assert.NotNull(loaded2);
    Assert.Equal(loaded.AcmeAccountResourceId, loaded2!.AcmeAccountResourceId);
    Assert.Equal(loaded.AccountId, loaded2.AccountId);
    Assert.Equal(loaded.Description, loaded2.Description);
    Assert.Equal(loaded.Contacts, loaded2.Contacts);
  }
}
