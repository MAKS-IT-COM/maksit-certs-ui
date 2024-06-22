using Microsoft.AspNetCore.Mvc;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase {
  private readonly ICacheRestService _cacheService;
  private readonly ICertsFlowService _certsFlowService;

  public AccountController(
      ICacheService cacheService,
      ICertsFlowService certsFlowService
  ) {
    _cacheService = cacheService;
    _certsFlowService = certsFlowService;
  }


  [HttpPost]
  public async Task<IActionResult> PostAccount([FromBody] PostAccountRequest requestData) {
    //var result = await _cacheService.PostAccountAsync(requestData);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  [HttpPut("{accountId:guid}")]
  public async Task<IActionResult> PutAccount(Guid accountId, [FromBody] PutAccountRequest requestData) {
    var result = await _cacheService.PutAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("{accountId:guid}")]
  public async Task<IActionResult> PatchAccount(Guid accountId, [FromBody] PatchAccountRequest requestData) {
    var result = await _cacheService.PatchAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("{accountd:guid}")]
  public async Task<IActionResult> DeleteAccount(Guid accountId) {
    var result = await _cacheService.DeleteAccountAsync(accountId);
    return result.ToActionResult();
  }

  #region Contacts

  [HttpGet("{accountId:guid}/contacts")]
  public async Task<IActionResult> GetContacts(Guid accountId) {
    var result = await _cacheService.GetContactsAsync(accountId);
    return result.ToActionResult();
  }

  [HttpPut("{accountId:guid}/contacts")]
  public async Task<IActionResult> PutContacts(Guid accountId, [FromBody] PutContactsRequest requestData) {
    var result = await _cacheService.PutContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("{accountId:guid}/contacts")]
  public async Task<IActionResult> PatchContacts(Guid accountId, [FromBody] PatchContactsRequest requestData) {
    var result = await _cacheService.PatchContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("{accountId:guid}/contact/{index:int}")]
  public async Task<IActionResult> DeleteContact(Guid accountId, int index) {
    var result = await _cacheService.DeleteContactAsync(accountId, index);
    return result.ToActionResult();
  }
  #endregion

  #region Hostnames

  [HttpGet("{accountId:guid}/hostnames")]
  public async Task<IActionResult> GetHostnames(Guid accountId) {
    var result = await _cacheService.GetHostnames(accountId);
    return result.ToActionResult();
  }

  [HttpPost("{accountId:guid}")]

  [HttpDelete("{accountId:guid}/hostname/{index:int}")]
  public async Task<IActionResult> DeleteHostname(Guid accountId, int index) {
    //var result = await _cacheService.DeleteHostnameAsync(accountId, index);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  #endregion
}
