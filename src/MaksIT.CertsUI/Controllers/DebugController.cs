using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/debug")]
[ServiceFilter(typeof(JwtOrApiKeyAuthorizationFilter))]
public sealed class DebugController(IRuntimeInstanceId runtimeInstance) : ControllerBase {

  /// <summary>
  /// Returns the runtime instance id used as PostgreSQL lease holder id.
  /// Useful for HA diagnostics and E2E verification behind load balancers.
  /// </summary>
  [HttpGet("runtime-instance-id")]
  public IActionResult GetRuntimeInstanceId() =>
    Ok(new { instanceId = runtimeInstance.InstanceId });
}

