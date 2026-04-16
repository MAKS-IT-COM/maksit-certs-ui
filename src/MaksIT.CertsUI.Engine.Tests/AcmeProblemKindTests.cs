using MaksIT.LetsEncrypt;
using Xunit;

namespace MaksIT.LetsEncrypt.Tests;

public class AcmeProblemKindTests {
  [Theory]
  [InlineData("urn:ietf:params:acme:error:rateLimited")]
  [InlineData("urn:ietf:params:acme:error:badNonce")]
  [InlineData("urn:ietf:params:acme:error:malformed")]
  [InlineData("urn:ietf:params:acme:error:serverInternal")]
  public void FromTypeUri_maps_rfc8555_uris(string uri) {
    var kind = AcmeProblemKind.FromTypeUri(uri);
    Assert.Equal(uri, kind.Name);
  }

  [Fact]
  public void FromTypeUri_unknown_or_empty_returns_unknown() {
    Assert.Same(AcmeProblemKind.Unknown, AcmeProblemKind.FromTypeUri(null));
    Assert.Same(AcmeProblemKind.Unknown, AcmeProblemKind.FromTypeUri(""));
    Assert.Same(AcmeProblemKind.Unknown, AcmeProblemKind.FromTypeUri("urn:ietf:params:acme:error:customVendorThing"));
  }

  [Fact]
  public void FromTypeUri_is_case_sensitive_for_full_uri() {
    Assert.Same(AcmeProblemKind.Unknown, AcmeProblemKind.FromTypeUri("urn:ietf:params:acme:error:RATELIMITED"));
  }
}
