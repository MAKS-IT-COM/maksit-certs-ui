namespace MaksIT.CertsUI.Engine.RuntimeCoordination;

/// <summary>
/// Canonical <b>lease holder</b> identity for this application instance: register exactly one implementation as a
/// <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton"/> so every component that acquires
/// or releases rows in <c>app_runtime_leases</c> uses the same <see cref="InstanceId"/> as <c>holder_id</c>.
/// </summary>
/// <remarks>
/// In Kubernetes, prefer a value stable for the pod lifetime (e.g. <c>POD_NAME</c> / <c>HOSTNAME</c>), not a value
/// that changes on every process start, so a replica can renew or release its own lease after restarts without
/// waiting for TTL expiry.
/// </remarks>
public interface IRuntimeInstanceId {
  /// <summary>Opaque string stored as <c>holder_id</c> in PostgreSQL runtime leases for this replica.</summary>
  string InstanceId { get; }
}
