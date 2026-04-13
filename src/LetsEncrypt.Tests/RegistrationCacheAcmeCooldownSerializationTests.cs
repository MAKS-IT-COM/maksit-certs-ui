using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Entities;
using Xunit;

namespace MaksIT.LetsEncrypt.Tests;

public class RegistrationCacheAcmeCooldownSerializationTests {
  [Fact]
  public void AcmeRenewalNotBeforeUtcByHostname_round_trips_json() {
    var until = new DateTimeOffset(2026, 4, 12, 17, 49, 19, TimeSpan.Zero);
    var cache = new RegistrationCache {
      AccountId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
      Description = "test",
      Contacts = ["ops@example.com"],
      IsStaging = true,
      ChallengeType = "http-01",
      AcmeRenewalNotBeforeUtcByHostname = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase) {
        ["cloud.example.com"] = until
      }
    };

    var json = cache.ToJson();
    var restored = json.ToObject<RegistrationCache>();

    Assert.NotNull(restored);
    Assert.NotNull(restored!.AcmeRenewalNotBeforeUtcByHostname);
    Assert.True(restored.AcmeRenewalNotBeforeUtcByHostname!.TryGetValue("cloud.example.com", out var v));
    Assert.Equal(until, v);
  }
}
