using MaksIT.Models.LetsEncryptServer.Common;
using MaksIT.Models.LetsEncryptServer.Identity.Login;
using MaksIT.Models.LetsEncryptServer.Identity.Logout;
using MaksIT.Models.LetsEncryptServer.Identity.User;
using MaksIT.Models.LetsEncryptServer.Identity.User.Search;
using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController(
  IIdentityService identityService
) : ControllerBase {

  private readonly IIdentityService _identityService = identityService;

  #region Search
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("search")]
  [ProducesResponseType(typeof(PagedResponse<SearchUserResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetUsers([FromBody] SearchUserRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.SearchUsersAsync(jwtTokenData, requestData);
    return result.ToActionResult();
  }
  #endregion

  #region Read
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpGet("user/{id:guid}")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetUser(Guid id) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.ReadUserAsync(jwtTokenData, id);
    return result.ToActionResult();
  }
  #endregion

  #region Create
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("user")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PostUser([FromBody] CreateUserRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.PostUserAsync(jwtTokenData, requestData);
    return result.ToActionResult();
  }
  #endregion

  #region Patch
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
  #endregion

  #region Delete
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpDelete("user/{id:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> DeleteUser(Guid id) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.DeleteUserAsync(jwtTokenData, id);
    return result.ToActionResult();
  }
  #endregion

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

  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("logout")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> Logout([FromBody] LogoutRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var result = await _identityService.Logout(jwtTokenDataResult.Value, requestData);
    return result.ToActionResult();
  }
  #endregion
}
