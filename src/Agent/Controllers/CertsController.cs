
using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MaksIT.Models.Agent.Requests;

namespace MaksIT.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class CertsController : ControllerBase {

  private readonly Configuration _appSettings;

  public CertsController(
     IOptions<Configuration> appSettings
  ) {
    _appSettings = appSettings.Value;
  }

  [HttpPost("[action]")]
  public IActionResult Upload([FromBody] CertsUploadRequest requestData) {
    if (!Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey)) {
      return Unauthorized("API Key is missing");
    }

    if (!_appSettings.ApiKey.Equals(extractedApiKey)) {
      return Unauthorized("Unauthorized client");
    }

    foreach (var (fileName, fileContent) in requestData.Certs) {
      System.IO.File.WriteAllText(Path.Combine(_appSettings.CertsPath, fileName), fileContent);
    }

    return Ok("Certificates uploaded successfully");
  }

}

