namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Paths and agent wiring for applying certificates after ACME issuance. The host maps these from configuration (e.g. appsettings).
/// </summary>
public interface ICertsFlowEngineConfiguration {
  string AcmeFolder { get; }
  string DataFolder { get; }
  string AgentServiceToReload { get; }
}
