using Microsoft.AspNetCore.Mvc;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Account.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api")]
public class AccountController : ControllerBase {
  private readonly IAccountRestService _accountService;

  public AccountController(
      IAccountService accountService
  ) {
    _accountService = accountService;
  }

  #region Accounts

  [HttpGet("accounts")]
  public async Task<IActionResult> GetAccounts() {
    var result = await _accountService.GetAccountsAsync();
    return result.ToActionResult();
  }

  #endregion

  #region Account

  [HttpPost("account")]
  public async Task<IActionResult> PostAccount([FromBody] PostAccountRequest requestData) {
    var result = await _accountService.PostAccountAsync(requestData);
    return result.ToActionResult();
  }

  [HttpPut("account/{accountId:guid}")]
  public async Task<IActionResult> PutAccount(Guid accountId, [FromBody] PutAccountRequest requestData) {
    var result = await _accountService.PutAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("account/{accountId:guid}")]
  public async Task<IActionResult> PatchAccount(Guid accountId, [FromBody] PatchAccountRequest requestData) {
    var result = await _accountService.PatchAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("account/{accountd:guid}")]
  public async Task<IActionResult> DeleteAccount(Guid accountId) {
    var result = await _accountService.DeleteAccountAsync(accountId);
    return result.ToActionResult();
  }

  #endregion

  #region Account Contacts

  [HttpGet("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> GetContacts(Guid accountId) {
    var result = await _accountService.GetContactsAsync(accountId);
    return result.ToActionResult();
  }

  [HttpPost("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> PostContacts(Guid accountId, [FromBody] PostContactsRequest requestData) {
    //var result = await _accountService.PostContactsAsync(accountId, requestData);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  [HttpPut("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> PutContacts(Guid accountId, [FromBody] PutContactsRequest requestData) {
    var result = await _accountService.PutContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpPatch("account/{accountId:guid}/contacts")]
  public async Task<IActionResult> PatchContacts(Guid accountId, [FromBody] PatchContactsRequest requestData) {
    var result = await _accountService.PatchContactsAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("account/{accountId:guid}/contact/{index:int}")]
  public async Task<IActionResult> DeleteContact(Guid accountId, int index) {
    var result = await _accountService.DeleteContactAsync(accountId, index);
    return result.ToActionResult();
  }
  #endregion

  #region Account Hostnames

  [HttpGet("{accountId:guid}/hostnames")]
  public async Task<IActionResult> GetHostnames(Guid accountId) {
    var result = await _accountService.GetHostnames(accountId);
    return result.ToActionResult();
  }

  [HttpPost("account/{accountId:guid}/hostnames")]
  public async Task<IActionResult> PostHostname(Guid accountId, [FromBody] PostHostnamesRequest requestData) {
    //var result = await _cacheService.PostHostnameAsync(accountId, requestData);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  [HttpPut("account/{accountId:guid}/hostnames")]
  public async Task<IActionResult> PutHostname(Guid accountId, [FromBody] PutHostnamesRequest requestData) {
    //var result = await _cacheService.PutHostnameAsync(accountId, requestData);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  [HttpPatch("account/{accountId:guid}/hostnames")]
  public async Task<IActionResult> PatchHostname(Guid accountId, [FromBody] PatchHostnamesRequest requestData) {
    //var result = await _cacheService.PatchHostnameAsync(accountId, requestData);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }


  [HttpDelete("account/{accountId:guid}/hostname/{index:int}")]
  public async Task<IActionResult> DeleteHostname(Guid accountId, int index) {
    //var result = await _cacheService.DeleteHostnameAsync(accountId, index);
    //return result.ToActionResult();

    return BadRequest("Not implemented");
  }

  #endregion
}
