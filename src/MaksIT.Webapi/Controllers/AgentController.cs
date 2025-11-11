using MaksIT.Webapi.Authorization.Filters;
using MaksIT.Webapi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LetsEncryptServer.Controllers;


[ApiController]
[Route("api")]
[ServiceFilter(typeof(JwtAuthorizationFilter))]
public class AgentController : ControllerBase {

  private readonly IAgentService _agentController;
  public AgentController(
      IAgentService agentController
  ) {
    _agentController = agentController;
  }

  [HttpGet("agent/test")]
  public async Task<IActionResult> Test() {
    var result = await _agentController.GetHelloWorld();
    return result.ToActionResult();
  }

}
