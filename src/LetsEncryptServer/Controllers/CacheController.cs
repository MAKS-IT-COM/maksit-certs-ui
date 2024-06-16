using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase {
  private readonly Configuration _appSettings;
  private readonly ICacheRestService _cacheService;

  public CacheController(
      IOptions<Configuration> appSettings,
      ICacheService cacheService
  ) {
    _appSettings = appSettings.Value;
    _cacheService = (ICacheRestService)cacheService;
  }

  [HttpGet("accounts")]
  public async Task<IActionResult> GetAccounts() {
    var result = await _cacheService.GetAccountsAsync();
    return result.ToActionResult();
  }

  #region Contacts

  [HttpGet("{accountId}/contacts")]
  public async Task<IActionResult> GetContacts(Guid accountId) {
    var result = await _cacheService.GetContactsAsync(accountId);
    return result.ToActionResult();
  }

  [HttpPut("{accountId}/contacts")]
  public async Task<IActionResult> PutContacts(Guid accountId, [FromBody] PutContactsRequest requestData) {
    var result = await _cacheService.PutContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("{accountId}/contacts")]
  public async Task<IActionResult> PatchContacts(Guid accountId, [FromBody] PatchContactRequest requestData) {
    var result = await _cacheService.PatchContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("{accountId}/contacts/{index}")]
  public async Task<IActionResult> DeleteContact(Guid accountId, int index) {
    var result = await _cacheService.DeleteContactAsync(accountId, index);
    return result.ToActionResult();
  }
  #endregion

  #region Hostnames

  [HttpGet("{accountId}/hostnames")]
  public async Task<IActionResult> GetHostnames(Guid accountId) {
    var result = await _cacheService.GetHostnames(accountId);
    return result.ToActionResult();
  }

  #endregion
}
