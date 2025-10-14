using MaksIT.Models.Agent.Requests;
using MaksIT.Results;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MaksIT.LetsEncryptServer.Services {

  public interface IAgentService {
    Task<Result> GetHelloWorld();
    Task<Result> UploadCerts(Dictionary<string, string> certs);
    Task<Result> ReloadService(string serviceName);
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

    public Task<Result> GetHelloWorld() {
      throw new NotImplementedException();
    }

    public async Task<Result> ReloadService(string serviceName) {
      var requestBody = new ServiceReloadRequest { ServiceName = serviceName };
      var endpoint = $"/Service/Reload";
      return await SendHttpRequest(requestBody, endpoint);
    }

    public async Task<Result> UploadCerts(Dictionary<string, string> certs) {
      var requestBody = new CertsUploadRequest { Certs = certs };
      var endpoint = $"/Certs/Upload";
      return await SendHttpRequest(requestBody, endpoint);
    }

    private async Task<Result> SendHttpRequest<T>(T requestBody, string endpoint) {
      try {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Agent.AgentHostname}:{_appSettings.Agent.AgentPort}{endpoint}") {
          Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", _appSettings.Agent.AgentKey);
        request.Headers.Add("accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
          return Result.Ok();
        }
        else {
          _logger.LogError($"Request to {endpoint} failed with status code: {response.StatusCode}");
          return Result.InternalServerError($"Request to {endpoint} failed with status code: {response.StatusCode}");
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex, "Something went wrong");
        return Result.InternalServerError("Something went wrong");
      }
    }
  }
}
