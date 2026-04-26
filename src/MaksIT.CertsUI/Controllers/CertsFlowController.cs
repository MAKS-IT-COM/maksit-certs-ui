using MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Mvc;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers {
  /// <summary>
  /// Certificates flow controller, used for granular testing purposes
  /// </summary>
  [ApiController]
  [Route("api/certs")]
  [ServiceFilter(typeof(JwtAuthorizationFilter))]
  public class CertsFlowController : ControllerBase {
    private readonly ICertsFlowService _certsFlowService;

    public CertsFlowController(ICertsFlowService certsFlowService) {
      _certsFlowService = certsFlowService;
    }

    [HttpPost("configure-client")]
    public async Task<IActionResult> ConfigureClient([FromBody] ConfigureClientRequest requestData) {
      var result = await _certsFlowService.ConfigureClientAsync(requestData.IsStaging);
      return result.ToCertsFlowActionResult();
    }

    [HttpGet("{sessionId}/terms-of-service")]
    public async Task<IActionResult> TermsOfService(Guid sessionId) {
      var result = await _certsFlowService.GetTermsOfServiceAsync(sessionId);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{sessionId}/init/{accountId?}")]
    public async Task<IActionResult> Init(Guid sessionId, Guid? accountId, [FromBody] InitRequest requestData) {
      var result = await _certsFlowService.InitAsync(sessionId, accountId, requestData.Description, requestData.Contacts);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{sessionId}/order")]
    public async Task<IActionResult> NewOrder(Guid sessionId, [FromBody] NewOrderRequest requestData) {
      var result = await _certsFlowService.NewOrderAsync(sessionId, requestData.Hostnames, requestData.ChallengeType);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{sessionId}/complete-challenges")]
    public async Task<IActionResult> CompleteChallenges(Guid sessionId) {
      var result = await _certsFlowService.CompleteChallengesAsync(sessionId);
      return result.ToCertsFlowActionResult();
    }

    [HttpGet("{sessionId}/order-status")]
    public async Task<IActionResult> GetOrder(Guid sessionId, [FromBody] GetOrderRequest requestData) {
      var result = await _certsFlowService.GetOrderAsync(sessionId, requestData.Hostnames);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{sessionId}/certificates/download")]
    public async Task<IActionResult> GetCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
      var result = await _certsFlowService.GetCertificatesAsync(sessionId, requestData.Hostnames);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{accountId}/certificates/apply")]
    public async Task<IActionResult> ApplyCertificates(Guid accountId) {
      var result = await _certsFlowService.ApplyCertificatesAsync(accountId);
      return result.ToCertsFlowActionResult();
    }

    [HttpPost("{sessionId}/certificates/revoke")]
    public async Task<IActionResult> RevokeCertificates(Guid sessionId, [FromBody] RevokeCertificatesRequest requestData) {
      var result = await _certsFlowService.RevokeCertificatesAsync(sessionId, requestData.Hostnames);
      return result.ToCertsFlowActionResult();
    }
  }
}
