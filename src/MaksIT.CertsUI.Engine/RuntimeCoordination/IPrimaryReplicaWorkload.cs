namespace MaksIT.CertsUI.Engine.RuntimeCoordination;

/// <summary>
/// True when this process is the elected primary replica (Postgres lease) and may run ACME orchestration and background renewal.
/// </summary>
public interface IPrimaryReplicaWorkload {
  bool IsPrimary { get; }
}
