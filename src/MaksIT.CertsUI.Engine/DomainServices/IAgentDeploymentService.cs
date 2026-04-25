using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// Deploys issued PEM certificates to a host agent and triggers a service reload. Implemented in the host (e.g. Web API calling an agent over HTTP).
/// </summary>
public interface IAgentDeploymentService {
  Task<Result> UploadCerts(Dictionary<string, string> certs);
  Task<Result> ReloadService(string serviceName);
}
