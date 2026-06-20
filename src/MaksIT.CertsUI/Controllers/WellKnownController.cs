using Microsoft.AspNetCore.Mvc;

using MaksIT.CertsUI.Services;


namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController(ICertsFlowService certsFlowService) : ControllerBase {


  [HttpGet("acme-challenge/{fileName}")]
  public async Task<IActionResult> AcmeChallenge(string fileName, CancellationToken cancellationToken) {
    var result = await certsFlowService.AcmeChallengeAsync(fileName, cancellationToken);
    if (!result.IsSuccess || result.Value == null)
      return NotFound();

    // Return as plain text, not as JSON
    return Content(result.Value, "text/plain");
  }

}

