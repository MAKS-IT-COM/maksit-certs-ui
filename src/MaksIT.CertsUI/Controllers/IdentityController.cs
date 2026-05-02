using Microsoft.AspNetCore.Mvc;
using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Models.Identity.Login;
using MaksIT.CertsUI.Models.Identity.Logout;
using MaksIT.CertsUI.Models.Identity.User;
using MaksIT.CertsUI.Services;
using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Models.Identity.User.Search;


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
  [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
  public IActionResult GetUsers([FromBody] SearchUserRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _identityService.SearchUsers(jwtTokenData, requestData);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("scopes/search")]
  [ProducesResponseType(typeof(PagedResponse<SearchUserEntityScopeResponse>), StatusCodes.Status200OK)]
  public IActionResult GetUserEntityScopes([FromBody] SearchUserEntityScopeRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _identityService.SearchUserEntityScopes(jwtTokenData, requestData);
    return result.ToActionResult();
  }
  #endregion

  #region Read
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpGet("user/{id:guid}")]
  [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
  public IActionResult GetUser(Guid id) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _identityService.ReadUser(jwtTokenData, id);
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
  public async Task<IActionResult> Logout([FromBody] LogoutRequest requetData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _identityService.Logout(jwtTokenData, requetData);
    return result.ToActionResult();
  }
  #endregion
}
