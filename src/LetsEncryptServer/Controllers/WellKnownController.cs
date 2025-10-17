using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MaksIT.LetsEncryptServer.Services;


namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase {

  private readonly ICertsRestChallengeService _certsFlowService;

  public WellKnownController(
    IOptions<Configuration> appSettings,
    ICertsFlowService certsFlowService
  ) {
    _certsFlowService = certsFlowService;
  }


  [HttpGet("acme-challenge/{fileName}")]
  public IActionResult AcmeChallenge(string fileName) {
    var result = _certsFlowService.AcmeChallenge(fileName);
    return result.ToActionResult();
  }

}

