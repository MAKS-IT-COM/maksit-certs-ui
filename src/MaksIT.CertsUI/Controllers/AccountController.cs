using Microsoft.AspNetCore.Mvc;
using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Models.CertsUI.Account.Requests;
using MaksIT.CertsUI.Models.CertsUI.Account.Responses;
using MaksIT.CertsUI.Services;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api")]
public class AccountController(
  IAccountService accountService
) : ControllerBase {

  #region Read
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("accounts")]
  [ProducesResponseType(typeof(GetAccountResponse[]), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAccounts() {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await accountService.GetAccountsAsync(certsUIAuthorizationData);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("account/{accountId:guid}")]
  [ProducesResponseType(typeof(GetAccountResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAccount(Guid accountId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await accountService.GetAccountAsync(certsUIAuthorizationData, accountId);
    return result.ToActionResult();
  }

  #endregion

  #region Create
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("account")]
  [ProducesResponseType(typeof(GetAccountResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PostAccount([FromBody] PostAccountRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await accountService.PostAccountAsync(certsUIAuthorizationData, requestData);
    return result.ToActionResult();
  }

  #endregion

  #region Patch
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPatch("account/{accountId:guid}")]
  [ProducesResponseType(typeof(GetAccountResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PatchAccount(Guid accountId, [FromBody] PatchAccountRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await accountService.PatchAccountAsync(certsUIAuthorizationData, accountId, requestData);
    return result.ToActionResult();
  }

  #endregion

  #region Delete
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpDelete("account/{accountId:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> DeleteAccount(Guid accountId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await accountService.DeleteAccountAsync(certsUIAuthorizationData, accountId);
    return result.ToActionResult();
  }

  #endregion
}
