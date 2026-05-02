using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/debug")]
public sealed class DebugController(
  IRuntimeInstanceId runtimeInstance
) : ControllerBase {

  /// <summary>
  /// Returns the runtime instance id used as PostgreSQL lease holder id.
  /// Useful for HA diagnostics and E2E verification behind load balancers.
  /// </summary>
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("runtime-instance-id")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public IActionResult GetRuntimeInstanceId() {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    return Ok(new { instanceId = runtimeInstance.InstanceId });
  }
}
