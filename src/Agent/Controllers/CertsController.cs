using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MaksIT.Models.Agent.Requests;
using MaksIT.Agent.AuthorizationFilters;

namespace MaksIT.Agent.Controllers;

[ApiController]
[Route("[controller]")]
[ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
public class CertsController : ControllerBase {

  private readonly Configuration _appSettings;

  public CertsController(
     IOptions<Configuration> appSettings
  ) {
    _appSettings = appSettings.Value;
  }

  [HttpPost("[action]")]
  public IActionResult Upload([FromBody] CertsUploadRequest requestData) {
    foreach (var (fileName, fileContent) in requestData.Certs) {
      System.IO.File.WriteAllText(Path.Combine(_appSettings.CertsPath, fileName), fileContent);
    }

    return Ok("Certificates uploaded successfully");
  }
}
