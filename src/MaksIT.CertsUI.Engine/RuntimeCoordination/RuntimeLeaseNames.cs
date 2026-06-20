namespace MaksIT.CertsUI.Engine.RuntimeCoordination;

/// <summary>
/// PostgreSQL <c>app_runtime_leases.lease_name</c> values for CertsUI (see <c>MaksIT.HAMode.Abstractions.IRuntimeLeaseService</c>).
/// </summary>
public static class RuntimeLeaseNames {
  public const string AcmeWriter = "certs-ui-acme-writer";

  /// <summary>Held only for coordination DDL + optional default-admin bootstrap; released when done (no renewal loop).</summary>
  public const string BootstrapCoordinator = "certs-ui-bootstrap";

  /// <summary>Held for one renewal sweep (purge + account passes); released after each sweep so any pod may run the next.</summary>
  public const string RenewalSweep = "certs-ui-renewal-sweep";
}
