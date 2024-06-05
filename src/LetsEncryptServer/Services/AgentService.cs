using DomainResults.Common;
using MaksIT.Models.Agent.Requests;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MaksIT.LetsEncryptServer.Services {

  public interface IAgentService {
    Task<IDomainResult> GetHelloWorld();
    Task<IDomainResult> UploadCerts(Dictionary<string, string> certs);
    Task<IDomainResult> ReloadService(string serviceName);
  }

  public class AgentService : IAgentService {

    private readonly Configuration _appSettings;
    private readonly ILogger<AgentService> _logger;
    private readonly HttpClient _httpClient;

    public AgentService(
      IOptions<Configuration> appSettings,
      ILogger<AgentService> logger,
      HttpClient httpClient
    ) {
      _appSettings = appSettings.Value;
      _logger = logger;
      _httpClient = httpClient;
    }

    public Task<IDomainResult> GetHelloWorld() {
      throw new NotImplementedException();
    }

    public async Task<IDomainResult> ReloadService(string serviceName) {
      var requestBody = new ServiceReloadRequest { ServiceName = serviceName };
      var endpoint = $"/Service/Reload";
      return await SendHttpRequest(requestBody, endpoint);
    }

    public async Task<IDomainResult> UploadCerts(Dictionary<string, string> certs) {
      var requestBody = new CertsUploadRequest { Certs = certs };
      var endpoint = $"/Certs/Upload";
      return await SendHttpRequest(requestBody, endpoint);
    }

    private async Task<IDomainResult> SendHttpRequest<T>(T requestBody, string endpoint) {
      try {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Agent.AgentHostname}:{_appSettings.Agent.AgentPort}{endpoint}") {
          Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", _appSettings.Agent.AgentKey);
        request.Headers.Add("accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
          return IDomainResult.Success();
        }
        else {
          _logger.LogError($"Request to {endpoint} failed with status code: {response.StatusCode}");
          return IDomainResult.Failed($"Request to {endpoint} failed with status code: {response.StatusCode}");
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex, "Something went wrong");
        return IDomainResult.Failed("Something went wrong");
      }
    }
  }
}
