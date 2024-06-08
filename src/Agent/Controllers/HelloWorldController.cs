using Microsoft.AspNetCore.Mvc;

using MaksIT.Agent.AuthorizationFilters;

namespace Agent.Controllers {

  [ApiController]
  [Route("[controller]")]
  [ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
  public class HelloWorldController : ControllerBase {

    [HttpGet]
    public IActionResult Get() {
      return Ok("Hello, World!");
    }
  }
}
