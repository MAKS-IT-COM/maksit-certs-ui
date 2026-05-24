using MaksIT.Core.Webapi.Models;
using MaksIT.CertsUI.Models.APIKeys;
using MaksIT.CertsUI.Models.APIKeys.Search;
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
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("search")]
  [ProducesResponseType(typeof(PagedResponse<SearchAPIKeyResponse>), StatusCodes.Status200OK)]
  public IActionResult GetAPIKeys([FromBody] SearchAPIKeyRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _apiKeyService.SearchApiKeys(jwtTokenData, requestData);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("search/entity-scopes")]
  [ProducesResponseType(typeof(PagedResponse<SearchApiKeyEntityScopeResponse>), StatusCodes.Status200OK)]
  public IActionResult SearchApiKeyEntityScopes([FromBody] SearchApiKeyEntityScopeRequest requestData) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _apiKeyService.SearchApiKeyEntityScopes(jwtTokenData, requestData);
    return result.ToActionResult();
  }

  #endregion

  #region Read
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("{apiKeyId:guid}")]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public IActionResult ReadAPIKey([FromRoute] Guid apiKeyId) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = _apiKeyService.ReadAPIKey(jwtTokenData, apiKeyId);
    return result.ToActionResult();
  }
  #endregion

  #region Create
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> CreateAPIKey([FromBody] CreateApiKeyRequest request) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.CreateAPIKeyAsync(jwtTokenData, request);
    return result.ToActionResult();
  }
  #endregion

  #region Patch
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPatch("{apiKeyId:guid}")]
  [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> PatchApiKey(Guid apiKeyId, [FromBody] PatchApiKeyRequest request) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.PatchAPIKeyAsync(jwtTokenData, apiKeyId, request);
    return result.ToActionResult();
  }
  #endregion

  #region Delete
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpDelete("{apiKeyId:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> DeleteAPIKey([FromRoute] Guid apiKeyId) {
    var jwtTokenDataResult = HttpContext.GetActingJwtTokenData();
    if (!jwtTokenDataResult.IsSuccess || jwtTokenDataResult.Value == null)
      return jwtTokenDataResult.ToActionResult();

    var jwtTokenData = jwtTokenDataResult.Value;

    var result = await _apiKeyService.DeleteAPIKeyAsync(jwtTokenData, apiKeyId);
    return result.ToActionResult();
  }
  #endregion
}
