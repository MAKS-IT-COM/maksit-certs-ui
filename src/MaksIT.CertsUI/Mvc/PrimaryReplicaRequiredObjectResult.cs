using MaksIT.CertsUI.Engine.DomainServices;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Mvc;

/// <summary>
/// HTTP 503 with <c>Retry-After</c> (delay-seconds) and RFC 7807 <see cref="ProblemDetails"/> for primary-replica routing.
/// </summary>
internal sealed class PrimaryReplicaRequiredObjectResult : ObjectResult {

  public PrimaryReplicaRequiredObjectResult(ProblemDetails problemDetails, int retryAfterSeconds) : base(problemDetails) {
    ArgumentOutOfRangeException.ThrowIfLessThan(retryAfterSeconds, 1);
    StatusCode = StatusCodes.Status503ServiceUnavailable;
    DeclaredType = typeof(ProblemDetails);
    RetryAfterSeconds = retryAfterSeconds;
  }

  public int RetryAfterSeconds { get; }

  public override Task ExecuteResultAsync(ActionContext context) {
    context.HttpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
    return base.ExecuteResultAsync(context);
  }

  internal static bool IsPrimaryReplicaResult(IReadOnlyList<string>? messages) =>
    messages is { Count: > 0 } && string.Equals(messages[0], CertsFlowPrimaryReplica.DiagnosticMarker, StringComparison.Ordinal);

  internal static IActionResult FromMessages(IReadOnlyList<string>? messages, int retryAfterSeconds) {
    var detail = (messages is { Count: > 1 } ? messages[1] : null) ?? "Only the primary replica runs this operation.";
    var pd = new ProblemDetails {
      Status = StatusCodes.Status503ServiceUnavailable,
      Title = "Primary replica required",
      Detail = detail,
      Type = CertsFlowPrimaryReplica.DiagnosticMarker,
    };
    pd.Extensions["retryAfterSeconds"] = retryAfterSeconds;
    return new PrimaryReplicaRequiredObjectResult(pd, retryAfterSeconds);
  }
}
