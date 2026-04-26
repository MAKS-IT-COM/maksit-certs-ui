namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Stable markers for <c>Result.ServiceUnavailable</c> when ACME is invoked on a non-primary replica.
/// The host maps these to HTTP 503 + <c>Retry-After</c> + RFC 7807 <c>ProblemDetails</c>.
/// </summary>
public static class CertsFlowPrimaryReplica {

  /// <summary>Machine-readable first line in result messages for detection in MVC.</summary>
  public const string DiagnosticMarker = "urn:maksit:certs-ui:primary-replica-required";

  public static readonly string[] ServiceUnavailableMessages = [
    DiagnosticMarker,
    "Only the elected primary Certs UI replica runs ACME orchestration. Retry after a short delay; use service session affinity (ClientIP) so interactive flows stay on the primary."
  ];
}
