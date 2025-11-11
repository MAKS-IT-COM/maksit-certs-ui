using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MaksIT.Webapi.Services;


namespace MaksIT.Webapi.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase {

  private readonly ICertsFlowService _certsFlowService;

  public WellKnownController(
    IOptions<Configuration> appSettings,
    ICertsFlowService certsFlowService
  ) {
    _certsFlowService = certsFlowService;
  }


  [HttpGet("acme-challenge/{fileName}")]
  public IActionResult AcmeChallenge(string fileName) {
    var result = _certsFlowService.AcmeChallenge(fileName);
    if (!result.IsSuccess || result.Value == null)
      return NotFound();

    // Return as plain text, not as JSON
    return Content(result.Value, "text/plain");
  }

}

