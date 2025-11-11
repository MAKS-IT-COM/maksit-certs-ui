using MaksIT.Models.LetsEncryptServer.Account.Requests;
using MaksIT.Webapi.Authorization.Filters;
using MaksIT.Webapi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.Webapi.Controllers;

[ApiController]
[Route("api")]
[ServiceFilter(typeof(JwtAuthorizationFilter))]
public class AccountController : ControllerBase {
  private readonly IAccountService _accountService;

  public AccountController(
      IAccountService accountService
  ) {
    _accountService = accountService;
  }

  #region Accounts

  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpGet("accounts")]
  public async Task<IActionResult> GetAccounts() {
    var result = await _accountService.GetAccountsAsync();
    return result.ToActionResult();
  }

  #endregion

  #region Account
  [HttpGet("account/{accountId:guid}")]
  public async Task<IActionResult> GetAccount(Guid accountId) {
    var result = await _accountService.GetAccountAsync(accountId);
    return result.ToActionResult();
  }

  [HttpPost("account")]
  public async Task<IActionResult> PostAccount([FromBody] PostAccountRequest requestData) {
    var result = await _accountService.PostAccountAsync(requestData);
    return result.ToActionResult();
  }

  [HttpPatch("account/{accountId:guid}")]
  public async Task<IActionResult> PatchAccount(Guid accountId, [FromBody] PatchAccountRequest requestData) {
    var result = await _accountService.PatchAccountAsync(accountId, requestData);
    return result.ToActionResult();
  }

  [HttpDelete("account/{accountId:guid}")]
  public async Task<IActionResult> DeleteAccount(Guid accountId) {
    var result = await _accountService.DeleteAccountAsync(accountId);
    return result.ToActionResult();
  }

  #endregion
}
