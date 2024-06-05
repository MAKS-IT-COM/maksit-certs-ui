using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using DomainResults.Mvc;

using MaksIT.LetsEncryptServer.Services;
using MaksIT.Models.LetsEncryptServer.Requests;


namespace MaksIT.LetsEncryptServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CertsFlowController : ControllerBase {

  private readonly IOptions<Configuration> _appSettings;
  private readonly ICertsFlowService _certsFlowService;

  public CertsFlowController(
    IOptions<Configuration> appSettings,
    ICertsFlowService certsFlowService
  ) {
    _appSettings = appSettings;
    _certsFlowService = certsFlowService;
  }

  /// <summary>
  /// Initialize certificate flow session
  /// </summary>
  /// <returns>sessionId</returns>
  [HttpPost("[action]")]
  public async Task<IActionResult> ConfigureClient() {
    var result = await _certsFlowService.ConfigureClientAsync();
    return result.ToActionResult();
  }

  [HttpGet("[action]/{sessionId}")]
  public IActionResult TermsOfService(Guid sessionId) {
    var result = _certsFlowService.GetTermsOfService(sessionId);
    return result.ToActionResult();
  }

  /// <summary>
  /// When new certificate session is created, create or retrieve cache data by accountId
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="accountId"></param>
  /// <param name="requestData"></param>
  /// <returns>accountId</returns>
  [HttpPost("[action]/{sessionId}/{accountId?}")]
  public async Task<IActionResult> Init(Guid sessionId, Guid? accountId, [FromBody] InitRequest requestData) {
    var resurt = await _certsFlowService.InitAsync(sessionId, accountId, requestData);
    return resurt.ToActionResult();
  }

  /// <summary>
  /// After account initialization create new order request
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="requestData"></param>
  /// <returns></returns>
  [HttpPost("[action]/{sessionId}")]
  public async Task<IActionResult> NewOrder(Guid sessionId, [FromBody] NewOrderRequest requestData) {
    var result = await _certsFlowService.NewOrderAsync(sessionId, requestData);
    return result.ToActionResult();
  }

  /// <summary>
  /// After new order request complete challenges
  /// </summary>
  /// <param name="sessionId"></param>
  /// <returns></returns>
  [HttpPost("[action]/{sessionId}")]
  public async Task<IActionResult> CompleteChallenges(Guid sessionId) {
    var result = await _certsFlowService.CompleteChallengesAsync(sessionId);
    return result.ToActionResult();
  }

  /// <summary>
  /// Get order status before certs retrieval
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="requestData"></param>
  /// <returns></returns>
  [HttpPost("[action]/{sessionId}")]
  public async Task<IActionResult> GetOrder(Guid sessionId, [FromBody] GetOrderRequest requestData) {
    var result = await _certsFlowService.GetOrderAsync(sessionId, requestData);
    return result.ToActionResult();
  }

  /// <summary>
  /// Download certs to local cache
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="requestData"></param>
  /// <returns></returns>
  [HttpPost("[action]/{sessionId}")]
  public async Task<IActionResult> GetCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
    var result = await _certsFlowService.GetCertificatesAsync(sessionId, requestData);
    return result.ToActionResult();
  }

  /// <summary>
  /// Apply certs from local cache to remote server
  /// </summary>
  /// <param name="sessionId"></param>
  /// <param name="requestData"></param>
  /// <returns></returns>
  [HttpPost("[action]/{sessionId}")]
  public async Task<IActionResult> ApplyCertificates(Guid sessionId, [FromBody] GetCertificatesRequest requestData) {
    var result = await _certsFlowService.ApplyCertificates(sessionId, requestData);
    return result.ToActionResult();
  }
}

