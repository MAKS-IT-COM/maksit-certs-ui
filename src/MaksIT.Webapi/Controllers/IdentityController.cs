using Microsoft.AspNetCore.Mvc;
using MaksIT.Webapi.Authorization.Extensions;
using MaksIT.Webapi.Authorization.Filters;
using MaksIT.Webapi.Services;

using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;


namespace MaksIT.Webapi.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController(
  IIdentityService identityService
) : ControllerBase {

  private readonly IIdentityService _identityService = identityService;


  /// <summary>
  /// Patch user data.
  /// </summary>
  /// <param name="id">Nullable Id as user can patch his own data</param>
  /// <param name="requestData"></param>
  /// <returns></returns>
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPatch("user/{id:guid}")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PatchUser(Guid id, [FromBody] PatchUserRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.PatchUserAsync(jwtTokenData, id, requestData);
    return result.ToActionResult();
  }


  #region Login/Refresh/Logout
  [HttpPost("login")]
  [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> Login([FromBody] LoginRequest requestData) {
    var result = await _identityService.LoginAsync(requestData);
    return result.ToActionResult();
  }

  [HttpPost("refresh")]
  [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest requestData) {
    var result = await _identityService.RefreshTokenAsync(requestData);
    return result.ToActionResult();
  }

  [HttpPost("logout")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> Logout([FromBody] LogoutRequest requestData) {
    var result = await _identityService.Logout(requestData);
    return result.ToActionResult();
  }
  #endregion
}
