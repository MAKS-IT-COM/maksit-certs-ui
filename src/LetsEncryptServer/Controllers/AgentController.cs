using MaksIT.LetsEncryptServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LetsEncryptServer.Controllers;


[ApiController]
[Route("api")]
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
