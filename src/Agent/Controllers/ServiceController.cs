using System.Diagnostics;
using MaksIT.Models.Agent.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MaksIT.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class ServiceController : ControllerBase {

  private readonly Configuration _appSettings;

  public ServiceController(
    IOptions<Configuration> appSettings
  ) {
    _appSettings = appSettings.Value;
  }

  [HttpPost("[action]")]
  public IActionResult Reload([FromBody] ServiceReloadRequest requestData) {
    var serviceName = requestData.ServiceName;

    if (!Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey)) {
      return Unauthorized("API Key is missing");
    }

    if (!_appSettings.ApiKey.Equals(extractedApiKey)) {
      return Unauthorized("Unauthorized client");
    }

    try {
      var processStartInfo = new ProcessStartInfo {
        FileName = "/bin/systemctl",
        Arguments = $"reload {serviceName}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using (var process = new Process { StartInfo = processStartInfo }) {
        process.Start();
        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0) {
          return StatusCode(500, $"Error reloading service: {error}");
        }

        return Ok($"Service {serviceName} reloaded successfully: {output}");
      }
    }
    catch (Exception ex) {
      return StatusCode(500, $"Exception: {ex.Message}");
    }
  }
}

