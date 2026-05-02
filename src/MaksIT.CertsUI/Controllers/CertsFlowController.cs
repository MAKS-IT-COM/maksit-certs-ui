using MaksIT.CertsUI.Models.CertsUI.CertsFlow.Requests;
using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

/// <summary>
/// Certificates flow controller, used for granular testing purposes
/// </summary>
[ApiController]
[Route("api/certs")]
public class CertsFlowController(
  ICertsFlowService certsFlowService
) : ControllerBase {
  private readonly ICertsFlowService _certsFlowService = certsFlowService;

  #region Create
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("configure-client")]
  public async Task<IActionResult> ConfigureClient([FromBody] ConfigureClientRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.ConfigureClientAsync(certsUIAuthorizationData, requestData.IsStaging);
    return result.ToActionResult();
  }
  #endregion

  #region Read
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("{sessionId}/terms-of-service")]
  public async Task<IActionResult> TermsOfService(Guid sessionId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.GetTermsOfServiceAsync(certsUIAuthorizationData, sessionId);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{sessionId}/order")]
  public async Task<IActionResult> NewOrder(Guid sessionId, [FromBody] NewOrderRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.NewOrderAsync(certsUIAuthorizationData, sessionId, requestData.Hostnames, requestData.ChallengeType);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("{sessionId}/order-status")]
  public async Task<IActionResult> GetOrder(Guid sessionId, [FromBody] GetOrderRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.GetOrderAsync(certsUIAuthorizationData, sessionId, requestData.Hostnames);
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{sessionId}/certificates/download")]
  public async Task<IActionResult> GetCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.GetCertificatesAsync(certsUIAuthorizationData, sessionId, requestData.Hostnames);
    return result.ToActionResult();
  }
  #endregion

  #region Init
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{sessionId}/init/{accountId?}")]
  public async Task<IActionResult> Init(Guid sessionId, Guid? accountId, [FromBody] InitRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.InitAsync(certsUIAuthorizationData, sessionId, accountId, requestData.Description, requestData.Contacts);
    return result.ToActionResult();
  }
  #endregion

  #region Complete
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{sessionId}/complete-challenges")]
  public async Task<IActionResult> CompleteChallenges(Guid sessionId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.CompleteChallengesAsync(certsUIAuthorizationData, sessionId);
    return result.ToActionResult();
  }
  #endregion

  #region Apply
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{accountId}/certificates/apply")]
  public async Task<IActionResult> ApplyCertificates(Guid accountId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.ApplyCertificatesAsync(certsUIAuthorizationData, accountId);
    return result.ToActionResult();
  }
  #endregion

  #region Revoke
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{sessionId}/certificates/revoke")]
  public async Task<IActionResult> RevokeCertificates(Guid sessionId, [FromBody] RevokeCertificatesRequest requestData) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _certsFlowService.RevokeCertificatesAsync(certsUIAuthorizationData, sessionId, requestData.Hostnames);
    return result.ToActionResult();
  }
  #endregion
}
