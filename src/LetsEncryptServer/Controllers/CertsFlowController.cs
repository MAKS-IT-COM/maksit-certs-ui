using Microsoft.AspNetCore.Mvc;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;

namespace MaksIT.LetsEncryptServer.Controllers {

  /// <summary>
  /// Certificates flow controller, used for granular testing purposes
  /// </summary>
  [ApiController]
  [Route("api/certs")]
  public class CertsFlowController : ControllerBase {

    private readonly ICertsFlowService _certsFlowService;

    public CertsFlowController(
        ICertsFlowService certsFlowService
    ) {
      _certsFlowService = certsFlowService;
    }

    /// <summary>
    /// Initialize certificate flow session
    /// </summary>
    /// <returns>sessionId</returns>
    [HttpPost("configure-client")]
    public async Task<IActionResult> ConfigureClient([FromBody] ConfigureClientRequest requestData) {
      var result = await _certsFlowService.ConfigureClientAsync(requestData);
      return result.ToActionResult();
    }

    /// <summary>
    /// Retrieves terms of service
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Terms of service</returns>
    [HttpGet("{sessionId}/terms-of-service")]
    public IActionResult TermsOfService(Guid sessionId) {
      var result = _certsFlowService.GetTermsOfService(sessionId);
      return result.ToActionResult();
    }

    /// <summary>
    /// When a new certificate session is created, create or retrieve cache data by accountId
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="accountId">Account ID</param>
    /// <param name="requestData">Request data</param>
    /// <returns>Account ID</returns>
    [HttpPost("{sessionId}/init/{accountId?}")]
    public async Task<IActionResult> Init(Guid sessionId, Guid? accountId, [FromBody] InitRequest requestData) {
      var result = await _certsFlowService.InitAsync(sessionId, accountId, requestData);
      return result.ToActionResult();
    }

    /// <summary>
    /// After account initialization, create a new order request
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="requestData">Request data</param>
    /// <returns>New order response</returns>
    [HttpPost("{sessionId}/order")]
    public async Task<IActionResult> NewOrder(Guid sessionId, [FromBody] NewOrderRequest requestData) {
      var result = await _certsFlowService.NewOrderAsync(sessionId, requestData);
      return result.ToActionResult();
    }

    /// <summary>
    /// Complete challenges for the new order
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Challenges completion response</returns>
    [HttpPost("{sessionId}/complete-challenges")]
    public async Task<IActionResult> CompleteChallenges(Guid sessionId) {
      var result = await _certsFlowService.CompleteChallengesAsync(sessionId);
      return result.ToActionResult();
    }

    /// <summary>
    /// Get order status before certificate retrieval
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="requestData">Request data</param>
    /// <returns>Order status</returns>
    [HttpGet("{sessionId}/order-status")]
    public async Task<IActionResult> GetOrder(Guid sessionId, [FromBody] GetOrderRequest requestData) {
      var result = await _certsFlowService.GetOrderAsync(sessionId, requestData);
      return result.ToActionResult();
    }

    /// <summary>
    /// Download certificates to local cache
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="requestData">Request data</param>
    /// <returns>Certificates download response</returns>
    [HttpPost("{sessionId}/certificates/download")]
    public async Task<IActionResult> GetCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
      var result = await _certsFlowService.GetCertificatesAsync(sessionId, requestData);
      return result.ToActionResult();
    }

    /// <summary>
    /// Apply certificates from local cache to remote server
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="requestData">Request data</param>
    /// <returns>Certificates application response</returns>
    [HttpPost("{sessionId}/certificates/apply")]
    public async Task<IActionResult> ApplyCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
      var result = await _certsFlowService.ApplyCertificatesAsync(sessionId, requestData);
      return result.ToActionResult();
    }
  }
}
