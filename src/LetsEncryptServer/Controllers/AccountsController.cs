using Microsoft.AspNetCore.Mvc;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Cache.Requests;

namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase {
  private readonly ICacheRestService _cacheService;
  private readonly ICertsFlowService _certsFlowService;

  public AccountsController(
      ICacheService cacheService,
      ICertsFlowService certsFlowService
  ) {
    _cacheService = cacheService;
    _certsFlowService = certsFlowService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAccounts() {
    var result = await _cacheService.GetAccountsAsync();
    return result.ToActionResult();
  }
}
