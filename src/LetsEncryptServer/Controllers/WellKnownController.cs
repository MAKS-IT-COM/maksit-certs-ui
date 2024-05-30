using MaksIT.LetsEncrypt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LetsEncryptServer.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase {

  private readonly Configuration _appSettings;
  private readonly ILetsEncryptService _letsEncryptService;

  private readonly string _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");

  public WellKnownController(
    IOptions<Configuration> appSettings,
    ILetsEncryptService letsEncryptService
  ) {
    _appSettings = appSettings.Value;
    _letsEncryptService = letsEncryptService;

    if (!Directory.Exists(_acmePath))
      Directory.CreateDirectory(_acmePath);
  }


  [HttpGet("acme-challenge/{fileName}")]
  public IActionResult AcmeChallenge(string fileName) {

    var fileContent = System.IO.File.ReadAllText(Path.Combine(_acmePath, fileName));
    if (fileContent == null)
      return NotFound();

    return Ok(fileContent);
  }

}

