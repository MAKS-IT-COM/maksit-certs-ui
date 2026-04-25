using MaksIT.Models.LetsEncryptServer.ApiKeys;
using MaksIT.Models.LetsEncryptServer.ApiKeys.Search;
using MaksIT.Models.LetsEncryptServer.Common;
using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/apikey")]
public class APIKeyController(
  IApiKeyService apiKeyService
) : ControllerBase {

  private readonly IApiKeyService _apiKeyService = apiKeyService;

  #region Search
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("search")]
  [ProducesResponseType(typeof(PagedResponse<SearchAPIKeyResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAPIKeys([FromBody] SearchAPIKeyRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.SearchApiKeysAsync(jwtTokenData, requestData);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost("scopes/search")]
  [ProducesResponseType(typeof(PagedResponse<SearchApiKeyEntityScopeResponse>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetApiKeyEntityScopes([FromBody] SearchApiKeyEntityScopeRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.SearchApiKeyEntityScopesAsync(jwtTokenData, requestData);
    return result.ToActionResult();
  }
  #endregion

  #region Read
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpGet("{apiKeyId:guid}")]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> ReadAPIKey([FromRoute] Guid apiKeyId) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.ReadAPIKeyAsync(jwtTokenData, apiKeyId);
    return result.ToActionResult();
  }
  #endregion

  #region Create
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPost]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> CreateAPIKey([FromBody] CreateApiKeyRequest request) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.CreateAPIKeyAsync(jwtTokenData, request);
    return result.ToActionResult();
  }
  #endregion

  #region Patch
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpPatch("{apiKeyId:guid}")]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PatchApiKey(Guid apiKeyId, [FromBody] PatchApiKeyRequest request) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.PatchAPIKeyAsync(jwtTokenData, apiKeyId, request);
    return result.ToActionResult();
  }
  #endregion

  #region Delete
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  [HttpDelete("{apiKeyId:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> DeleteAPIKey([FromRoute] Guid apiKeyId) {
    var jwtTokenDataResult = HttpContext.GetJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.DeleteAPIKeyAsync(jwtTokenData, apiKeyId);
    return result.ToActionResult();
  }
  #endregion
}
