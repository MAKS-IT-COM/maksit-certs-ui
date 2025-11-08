using MaksIT.LetsEncryptServer.Services;
using Microsoft.AspNetCore.Mvc;
using Models.LetsEncryptServer.Identity.Login;
using Models.LetsEncryptServer.Identity.Logout;


namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController(
  IIdentityService identityService
) : ControllerBase {

  private readonly IIdentityService _identityService = identityService;

  #region Login/Refresh/Logout
  [HttpPost("login")]
  [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> Login([FromBody] LoginRequest requestData) {
    var result = await _identityService.LoginAsync(requestData);
    return result.ToActionResult();
  }

  //[HttpPost("refresh")]
  //[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
  //public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest requestData) {
  //  var result = await _identityService.RefreshTokenAsync(requestData);
  //  return result.ToActionResult();
  //}

  //[ServiceFilter(typeof(JwtAuthorizationFilter))]
  //[HttpPost("logout")]
  //[ProducesResponseType(StatusCodes.Status200OK)]
  //public async Task<IActionResult> Logout([FromBody] LogoutRequest requetData) {
  //  var jwtTokenDataResult = HttpContext.GetJwtTokenData();
  //  if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
  //    return jwtTokenDataResult.ToActionResult();

  //  var jwtTokenData = jwtTokenDataResult.Value;

  //  var result = await _identityService.Logout(jwtTokenData, requetData);
  //  return result.ToActionResult();
  //}
  #endregion



}
