

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController {

  private readonly Configuration _appSettings;
  private readonly ICacheService _cacheService;

  public CacheController(
    IOptions<Configuration> appSettings,
    ICacheService cacheService

  ) {
    _appSettings = appSettings.Value;
    _cacheService = cacheService;
  }


  [HttpGet("[action]/{accountId}")]
  public async Task<IActionResult> GetContacts(Guid accountId) {
    var result = await _cacheService.GetContactsAsync(accountId);
    return result.ToActionResult();
  }


  [HttpPost("[action]/{accountId}")]
  public async Task<IActionResult> SetContacts(Guid accountId, [FromBody] SetContactsRequest requestData) {
    var result = await _cacheService.SetContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }
}

