using MaksIT.Results;
using MaksIT.Results.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Mvc;

/// <summary>
/// Maps ACME domain results to HTTP: primary-replica required becomes 503 + <c>Retry-After</c> + ProblemDetails.
/// </summary>
public static class CertsFlowResultExtensions {

  /// <summary>Default retry hint for clients and caches (seconds).</summary>
  public const int DefaultPrimaryReplicaRetryAfterSeconds = 2;

  public static IActionResult ToCertsFlowActionResult(this Result result) {
    if (!result.IsSuccess && PrimaryReplicaRequiredObjectResult.IsPrimaryReplicaResult(result.Messages))
      return PrimaryReplicaRequiredObjectResult.FromMessages(result.Messages, DefaultPrimaryReplicaRetryAfterSeconds);
    return result.ToActionResult();
  }

  public static IActionResult ToCertsFlowActionResult<T>(this Result<T?> result) {
    if (!result.IsSuccess && PrimaryReplicaRequiredObjectResult.IsPrimaryReplicaResult(result.Messages))
      return PrimaryReplicaRequiredObjectResult.FromMessages(result.Messages, DefaultPrimaryReplicaRetryAfterSeconds);
    return result.ToActionResult();
  }
}
