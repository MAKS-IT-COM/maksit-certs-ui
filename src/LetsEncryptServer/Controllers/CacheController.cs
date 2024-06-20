using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase {
  private readonly ICacheRestService _cacheService;

  public CacheController(
      ICacheService cacheService
  ) {
    _cacheService = cacheService;
  }

  [HttpGet("accounts")]
  public async Task<IActionResult> GetAccounts() {
    var result = await _cacheService.GetAccountsAsync();
    return result.ToActionResult();
  }

  [HttpPut("account/{accountId:guid}")]
  public async Task<IActionResult> PutAccount(Guid accountId, [FromBody] PutAccountRequest requestData) {
    var result = await _cacheService.PutAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("account/{accountId:guid}")]
  public async Task<IActionResult> PatchAccount(Guid accountId, [FromBody] PatchAccountRequest requestData) {
    var result = await _cacheService.PatchAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  #region Contacts

  [HttpGet("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> GetContacts(Guid accountId) {
    var result = await _cacheService.GetContactsAsync(accountId);
    return result.ToActionResult();
  }

  [HttpPut("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> PutContacts(Guid accountId, [FromBody] PutContactsRequest requestData) {
    var result = await _cacheService.PutContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> PatchContacts(Guid accountId, [FromBody] PatchContactsRequest requestData) {
    var result = await _cacheService.PatchContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("account/{accountId:guid}/contact/{index:int}")]
  public async Task<IActionResult> DeleteContact(Guid accountId, int index) {
    var result = await _cacheService.DeleteContactAsync(accountId, index);
    return result.ToActionResult();
  }
  #endregion

  #region Hostnames

  [HttpGet("account/{accountId:guid}/hostnames")]
  public async Task<IActionResult> GetHostnames(Guid accountId) {
    var result = await _cacheService.GetHostnames(accountId);
    return result.ToActionResult();
  }

  #endregion
}
