using System.Net.Http.Headers;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;
using Xunit;

namespace MaksIT.CertsUI.Engine.Tests;

public class AcmeRetryAfterParserTests {
  [Fact]
  public void TryParseRetryAfterHttpHeader_reads_delta_seconds() {
    using var response = new HttpResponseMessage();
    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(90));

    var parsed = AcmeRetryAfterParser.TryParseRetryAfterHttpHeader(response);

    Assert.NotNull(parsed);
    var skew = (parsed.Value - DateTimeOffset.UtcNow.AddSeconds(90)).TotalSeconds;
    Assert.InRange(skew, -2, 2);
  }

  [Fact]
  public void TryParseRetryAfterHttpHeader_reads_absolute_date() {
    using var response = new HttpResponseMessage();
    var when = new DateTimeOffset(2026, 4, 12, 17, 49, 19, TimeSpan.Zero);
    response.Headers.RetryAfter = new RetryConditionHeaderValue(when);

    var parsed = AcmeRetryAfterParser.TryParseRetryAfterHttpHeader(response);

    Assert.Equal(when, parsed);
  }

  [Fact]
  public void TryParseRetryAfterFromDetail_parses_lets_encrypt_sample() {
    const string detail =
      "urn:ietf:params:acme:error:rateLimited: too many failed authorizations (5) for \"cloud.maks-it.com\" in the last 1h0m0s, retry after 2026-04-12 17:49:19 UTC: see https://letsencrypt.org/docs/rate-limits/";

    var parsed = AcmeRetryAfterParser.TryParseRetryAfterFromDetail(detail);

    Assert.NotNull(parsed);
    Assert.Equal(2026, parsed.Value.Year);
    Assert.Equal(4, parsed.Value.Month);
    Assert.Equal(12, parsed.Value.Day);
    Assert.Equal(17, parsed.Value.Hour);
    Assert.Equal(49, parsed.Value.Minute);
  }

  [Fact]
  public void TryParseRateLimitedHostname_extracts_identifier() {
    const string detail =
      "too many failed authorizations (5) for \"cloud.maks-it.com\" in the last 1h0m0s, retry after 2026-04-12 17:49:19 UTC";

    var host = AcmeRetryAfterParser.TryParseRateLimitedHostname(detail);

    Assert.Equal("cloud.maks-it.com", host);
  }

  [Fact]
  public void TryCombineRetryAfterUtc_takes_later_of_header_and_detail() {
    using var response = new HttpResponseMessage();
    response.Headers.RetryAfter = new RetryConditionHeaderValue(new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero));

    var problem = new Problem {
      Type = "urn:ietf:params:acme:error:rateLimited",
      Detail = "retry after 2026-04-12 18:00:00 UTC",
      RawJson = ""
    };

    var combined = AcmeRetryAfterParser.TryCombineRetryAfterUtc(response, problem);

    Assert.NotNull(combined);
    Assert.Equal(18, combined.Value.Hour);
  }
}
