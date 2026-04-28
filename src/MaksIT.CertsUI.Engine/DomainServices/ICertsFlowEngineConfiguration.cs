namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Agent wiring after ACME issuance. Interactive ACME and HTTP-01 state live in PostgreSQL, not on local paths.
/// </summary>
public interface ICertsFlowEngineConfiguration {
  string AgentServiceToReload { get; }
}
