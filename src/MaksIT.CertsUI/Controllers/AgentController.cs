using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Models.Agent.Responses;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController(
  IAgentService agentService
) : ControllerBase {

  private readonly IAgentService _agentService = agentService;

  #region Read
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("test")]
  [ProducesResponseType(typeof(HelloWorldResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> Test() {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _agentService.GetHelloWorld(certsUIAuthorizationData);
    return result.ToActionResult();
  }
  #endregion
}
