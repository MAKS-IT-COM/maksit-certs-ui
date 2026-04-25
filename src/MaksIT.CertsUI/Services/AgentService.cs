using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Models.Agent.Requests;
using MaksIT.Results;
using MaksIT.CertsUI.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaksIT.Models.Agent.Responses;
using System.Text;
using System.Text.Json;

namespace MaksIT.CertsUI.Services {

  public interface IAgentService {
    Task<Result<HelloWorldResponse?>> GetHelloWorld();
    Task<Result> UploadCerts(Dictionary<string, string> certs);
    Task<Result> ReloadService(string serviceName);
  }

  public class AgentService(
    IOptions<Configuration> appSettings,
    ILogger<AgentService> logger,
    HttpClient httpClient
  ) : ServiceBase(
    logger,
    appSettings
  ), IAgentService, IAgentDeploymentService {

    public async Task<Result<HelloWorldResponse?>> GetHelloWorld() {
      try {
        var endpoint = $"/HelloWorld";

        var fullAddress = $"{_appSettings.CertsUIEngineConfiguration.Agent.AgentHostname}:{_appSettings.CertsUIEngineConfiguration.Agent.AgentPort}{endpoint}";

        var request = new HttpRequestMessage(HttpMethod.Get, fullAddress);
        request.Headers.Add("x-api-key", _appSettings.CertsUIEngineConfiguration.Agent.AgentKey);

        logger.LogInformation($"Sending GET request to {fullAddress}");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
          var content = await response.Content.ReadAsStringAsync();

          return Result<HelloWorldResponse?>.Ok(new HelloWorldResponse {
            Message = content
          });
        }
        else {
          logger.LogError($"Request to {endpoint} failed with status code: {response.StatusCode}");
          return Result<HelloWorldResponse?>.InternalServerError(null, $"Request to {endpoint} failed with status code: {response.StatusCode}");
        }

      }
      catch (Exception ex) {
        List<string> messages = new() { "Something went wrong" };

        logger.LogError(ex, messages.FirstOrDefault());

        messages.Add(ex.Message);

        return Result<HelloWorldResponse?>.InternalServerError(null, [.. messages]);
      }
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.CertsUIEngineConfiguration.Agent.AgentHostname}:{_appSettings.CertsUIEngineConfiguration.Agent.AgentPort}{endpoint}") {
          Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", _appSettings.CertsUIEngineConfiguration.Agent.AgentKey);
        request.Headers.Add("accept", "application/json");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
          return Result.Ok();
        }
        else {
          logger.LogError($"Request to {endpoint} failed with status code: {response.StatusCode}");
          return Result.InternalServerError($"Request to {endpoint} failed with status code: {response.StatusCode}");
        }
      }
      catch (Exception ex) {
        List<string> messages = new() { "Something went wrong" };

        logger.LogError(ex, messages.FirstOrDefault());

        messages.Add(ex.Message);

        return Result.InternalServerError([.. messages]);
      }
    }
  }
}
