using DomainResults.Mvc;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncryptServer.Models.Requests;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LetsEncryptServer.Controllers;

public class LetsEncryptSession {
  public RegistrationCache? RegistrationCache { get; set; }
  public Order? CurrentOrder { get; set; }
  public List<AuthorizationChallenge>? Challenges { get; set; }
  public string[] Hostnames { get; set; }
}

[ApiController]
[Route("[controller]")]
public class CertsFlowController : ControllerBase {

  private readonly Configuration _appSettings;
  private readonly IMemoryCache _memoryCache;
  private readonly ILetsEncryptService _letsEncryptService;

  private readonly string _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");
  private readonly string _certPath = Path.Combine();

  MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    SlidingExpiration = TimeSpan.FromMinutes(2)
  };

  public CertsFlowController(
    IOptions<Configuration> appSettings,
    IMemoryCache memoryCache,
    ILetsEncryptService letsEncryptService
  ) {
    _memoryCache = memoryCache;
    _appSettings = appSettings.Value;
    _letsEncryptService = letsEncryptService;

    if (!Directory.Exists(_acmePath))
      Directory.CreateDirectory(_acmePath);

    Console.WriteLine(_acmePath);
  }

  [HttpGet("[action]")]
  public async Task<IActionResult> TermsOfService() {
    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();

    return Ok(config.Meta.TermsOfService);
  }


  [HttpPost("[action]")]
  public async Task<IActionResult> Init([FromBody] InitRequest requestData) {

    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();

    var (cache, cacheResult) = await _letsEncryptService.Init(config.NewAccount, config.NewNonce, requestData.Contacts);
    if(!cacheResult.IsSuccess || cache == null)
      return cacheResult.ToActionResult();

    var cacheData = new LetsEncryptSession {
      RegistrationCache = cache,
    };

    var accountId = Guid.NewGuid().ToString();

    _memoryCache.Set(accountId, cacheData, _cacheEntryOptions);

    return Ok(accountId);
  }

  [HttpPost("[action]/{accountId}")]
  public async Task<IActionResult> NewOrder(string accountId, [FromBody] NewOrderRequest requestData) {

   var cacheData = (LetsEncryptSession?)_memoryCache.Get(accountId);
    if (cacheData?.RegistrationCache?.AccountKey == null)
      return BadRequest();

    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();


    var (orderData, newOrderResult) = await _letsEncryptService.NewOrder(
      config.NewOrder,
      config.NewNonce,
      cacheData.RegistrationCache.AccountKey,
      cacheData.RegistrationCache.Location.ToString(),
      requestData.Hostnames,
      requestData.ChallengeType);

    if (!newOrderResult.IsSuccess)
      return newOrderResult.ToActionResult();

    var(currentOrder, results, challenges) = orderData;

    if (results?.Count == 0)
      return StatusCode(500);

    // TODO: save results to disk
    var fullPaths = new List<string>();
    foreach (var result in results) {
      string[] splitToken = result.Value.Split('.');

      System.IO.File.WriteAllText(Path.Combine(_acmePath, splitToken[0]), result.Value);

      fullPaths.Add(splitToken[0]);
    }

    cacheData.CurrentOrder = currentOrder;
    cacheData.Challenges = challenges;
    cacheData.Hostnames = requestData.Hostnames;

    _memoryCache.Set(accountId, cacheData, _cacheEntryOptions);

    return Ok(fullPaths);
  }

  [HttpPut("[action]/{accountId}")]
  public async Task<IActionResult> CompleteChallenges(string accountId) {

    var cacheData = (LetsEncryptSession?)_memoryCache.Get(accountId);
    if (cacheData?.RegistrationCache?.AccountKey == null)
      return BadRequest();

    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();

    var challengeResult = await _letsEncryptService.CompleteChallenges(
      config.NewNonce,
      cacheData.RegistrationCache.AccountKey,
      cacheData.RegistrationCache.Location.ToString(),
      cacheData.CurrentOrder,
      cacheData.Challenges
    );

    if (!challengeResult.IsSuccess)
      return challengeResult.ToActionResult();

    return Ok();
  }

  [HttpGet("[action]/{accountId}")]
  public async Task<IActionResult> GetOrder(string accountId) {

    var cacheData = (LetsEncryptSession?)_memoryCache.Get(accountId);
    if (cacheData?.RegistrationCache?.AccountKey == null)
      return BadRequest();

    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();


    var (currentOrder, currentOrderResult) = await _letsEncryptService.GetOrder(
      config.NewOrder,
      config.NewNonce,
      cacheData.RegistrationCache.AccountKey,
      cacheData.RegistrationCache.Location.ToString(),
      cacheData.Hostnames
     );

     if(!currentOrderResult.IsSuccess)
      return currentOrderResult.ToActionResult();

    cacheData.CurrentOrder = currentOrder;

    _memoryCache.Set(accountId, cacheData, _cacheEntryOptions);

    return Ok();
  }

  [HttpPost("[action]/{accountId}")]
  public async Task<IActionResult> GetCertificate(string accountId) {

    var cacheData = (LetsEncryptSession?)_memoryCache.Get(accountId);
    if (cacheData?.RegistrationCache?.AccountKey == null)
      return BadRequest();

    var (config, configResult) = await _letsEncryptService.ConfigureClient("https://acme-staging-v02.api.letsencrypt.org/directory");
    if (!configResult.IsSuccess || config == null)
      return configResult.ToActionResult();

    var (cachedCerts, certsResult) = await _letsEncryptService.GetCertificate(
      config.NewOrder,
      config.NewNonce,
      cacheData.RegistrationCache.AccountKey,
      cacheData.CurrentOrder,
      cacheData.RegistrationCache.Location.ToString(),
      cacheData.Hostnames
    );

    if (!certsResult.IsSuccess || cachedCerts == null)
      return certsResult.ToActionResult();

    // TODO: write certs to filesystem
    foreach (var (subject, cachedCert) in cachedCerts) {
      var cert = new X509Certificate2(Encoding.UTF8.GetBytes(cachedCert.Cert));
    }

    if (!certsResult.IsSuccess)
      return BadRequest();

    return Ok();
  }


}

