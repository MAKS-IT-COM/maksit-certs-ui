using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

/// <summary>
/// Thrown when the ACME server returns a problem document or challenge error.
/// </summary>
/// <remarks>
/// <see cref="Response"/> implements <see cref="IDisposable"/>. <see cref="MaksIT.CertsUI.Engine.Services.LetsEncryptService"/> disposes it
/// when mapping this exception to a <see cref="MaksIT.Core.Results.Result"/>; any other handler must dispose it to avoid holding connections.
/// </remarks>
public class LetsEncrytException : Exception {

  public Problem? Problem { get; }

  /// <summary>HTTP response that carried the problem (must be disposed if this exception is not handled by <see cref="MaksIT.CertsUI.Engine.Services.LetsEncryptService"/>).</summary>
  public HttpResponseMessage Response { get; }

  /// <summary>Classified <c>type</c> from the ACME problem document (RFC 8555 section 6.7).</summary>
  public AcmeProblemKind ProblemKind { get; }

  /// <summary>Combined Retry-After from HTTP header and problem detail, when present.</summary>
  public DateTimeOffset? RetryAfterUtc { get; }

  /// <summary>Hostname from Let's Encrypt rate-limit <c>detail</c>, when parseable.</summary>
  public string? RateLimitedIdentifier { get; }

  public bool IsRateLimited => ProblemKind == AcmeProblemKind.RateLimited;

  public LetsEncrytException(
    Problem? problem,
    HttpResponseMessage response
  ) : base(problem != null
      ? $"{problem.Type}: {problem.Detail}"
      : $"HTTP {(int)(response ?? throw new ArgumentNullException(nameof(response))).StatusCode}") {

    ArgumentNullException.ThrowIfNull(response);
    Problem = problem;
    Response = response;
    ProblemKind = AcmeProblemKind.FromTypeUri(problem?.Type);
    RetryAfterUtc = AcmeRetryAfterParser.TryCombineRetryAfterUtc(response, problem);
    if (ProblemKind == AcmeProblemKind.RateLimited)
      RateLimitedIdentifier = AcmeRetryAfterParser.TryParseRateLimitedHostname(problem?.Detail);
  }
}
