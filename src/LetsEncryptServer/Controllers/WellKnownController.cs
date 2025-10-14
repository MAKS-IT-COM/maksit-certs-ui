using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MaksIT.LetsEncryptServer.Services;


namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase {

  private readonly Configuration _appSettings;
  private readonly ICertsRestChallengeService _certsFlowService;

  private readonly string _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");

  public WellKnownController(
    IOptions<Configuration> appSettings,
    ICertsFlowService certsFlowService
  ) {
    _appSettings = appSettings.Value;
    _certsFlowService = certsFlowService;

    if (!Directory.Exists(_acmePath))
      Directory.CreateDirectory(_acmePath);
  }


  [HttpGet("acme-challenge/{fileName}")]
  public IActionResult AcmeChallenge(string fileName) {
    var result = _certsFlowService.AcmeChallenge(fileName);
    return result.ToActionResult();
  }

}

