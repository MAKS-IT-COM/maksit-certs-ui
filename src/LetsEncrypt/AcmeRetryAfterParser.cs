using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using MaksIT.LetsEncrypt.Models.Responses;

namespace MaksIT.LetsEncrypt;

internal static class AcmeRetryAfterParser {
  private static readonly Regex RetryAfterDetailRegex = new(
    @"retry\s+after\s+(?<ts>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+UTC",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  private static readonly Regex RateLimitedHostRegex = new(
    @"for\s+""(?<host>[^""]+)""",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  internal static DateTimeOffset? TryParseRetryAfterHttpHeader(HttpResponseMessage? response) {
    if (response?.Headers.RetryAfter is not { } ra)
      return null;

    if (ra.Date is { } absolute)
      return new DateTimeOffset(absolute.UtcDateTime, TimeSpan.Zero);

    if (ra.Delta is { } delta)
      return DateTimeOffset.UtcNow + delta;

    return null;
  }

  internal static DateTimeOffset? TryParseRetryAfterFromDetail(string? detail) {
    if (string.IsNullOrEmpty(detail))
      return null;

    var m = RetryAfterDetailRegex.Match(detail);
    if (!m.Success)
      return null;

    var ts = m.Groups["ts"].Value;
    if (DateTimeOffset.TryParseExact(ts, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
      return dto;

    return null;
  }

  /// <summary>
  /// Latest of header and detail-derived times (stricter / later wins). Null if neither present.
  /// </summary>
  internal static DateTimeOffset? TryCombineRetryAfterUtc(HttpResponseMessage? response, Problem? problem) {
    var fromHeader = TryParseRetryAfterHttpHeader(response);
    var fromDetail = TryParseRetryAfterFromDetail(problem?.Detail);

    if (fromHeader.HasValue && fromDetail.HasValue)
      return fromHeader.Value > fromDetail.Value ? fromHeader.Value : fromDetail.Value;

    return fromHeader ?? fromDetail;
  }

  internal static string? TryParseRateLimitedHostname(string? detail) {
    if (string.IsNullOrEmpty(detail))
      return null;

    var m = RateLimitedHostRegex.Match(detail);
    return m.Success ? m.Groups["host"].Value : null;
  }
}
