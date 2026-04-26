namespace MaksIT.CertsUI.Engine.RuntimeCoordination;

/// <summary>PostgreSQL <c>app_runtime_leases.lease_name</c> values.</summary>
public static class RuntimeLeaseNames {
  public const string AcmeWriter = "certs-ui-acme-writer";

  /// <summary>Single elected instance: identity bootstrap, ACME orchestration, and background renewal.</summary>
  public const string PrimaryReplica = "certs-ui-primary";
}
