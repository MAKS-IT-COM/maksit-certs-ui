using MaksIT.CertsUI.Engine.RuntimeCoordination;

namespace MaksIT.CertsUI.Infrastructure;

/// <summary>
/// Computes <see cref="IRuntimeInstanceId.InstanceId"/> once at construction. Must be registered as a singleton
/// so all lease acquire/release paths share the same PostgreSQL <c>holder_id</c>.
/// </summary>
public sealed class RuntimeInstanceIdProvider : IRuntimeInstanceId {
  public string InstanceId { get; } = Build();

  /// <summary>
  /// In-cluster: stable per pod (no PID) so an in-place process restart can still match <c>holder_id</c> in
  /// <c>app_runtime_leases</c>. Outside Kubernetes: append PID so two local processes on one machine do not collide.
  /// </summary>
  private static string Build() {
    var logicalHost =
      Environment.GetEnvironmentVariable("POD_NAME")
      ?? Environment.GetEnvironmentVariable("HOSTNAME")
      ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
      ?? Environment.MachineName;

    if (RunsInKubernetes())
      return logicalHost;

    return $"{logicalHost}-{Environment.ProcessId}";
  }

  /// <summary>Kubernetes sets this for every pod; use it instead of guessing from hostname alone.</summary>
  private static bool RunsInKubernetes() =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));
}
