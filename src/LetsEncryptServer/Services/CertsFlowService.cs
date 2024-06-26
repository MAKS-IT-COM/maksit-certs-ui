using Microsoft.Extensions.Options;

using DomainResults.Common;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Services;
using MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;
using System.Security.Cryptography;


namespace MaksIT.LetsEncryptServer.Services;

public interface ICertsCommonService {

  (string?, IDomainResult) GetTermsOfService(Guid sessionId);
  Task<IDomainResult> CompleteChallengesAsync(Guid sessionId);
}

public interface ICertsInternalService : ICertsCommonService {
  Task<(Guid?, IDomainResult)> ConfigureClientAsync(bool isStaging);
  Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType);
  Task<IDomainResult> GetOrderAsync(Guid sessionId, string[] hostnames);
  Task<IDomainResult> GetCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<(Dictionary<string, string>?, IDomainResult)> ApplyCertificatesAsync(Guid sessionId, string[] hostnames);
}

public interface ICertsRestService : ICertsCommonService {
  Task<(Guid?, IDomainResult)> ConfigureClientAsync(ConfigureClientRequest requestData);
  Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData);
  Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, NewOrderRequest requestData);
  Task<IDomainResult> GetOrderAsync(Guid sessionId, GetOrderRequest requestData);
  Task<IDomainResult> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData);
  Task<(Dictionary<string, string>?, IDomainResult)> ApplyCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData);
}

public interface ICertsRestChallengeService {
  (string?, IDomainResult) AcmeChallenge(string fileName);
}

public interface ICertsFlowService
  : ICertsInternalService,
    ICertsRestService,
    ICertsRestChallengeService { }

public class CertsFlowService : ICertsFlowService {

  private readonly Configuration _appSettings;
  private readonly ILogger<CertsFlowService> _logger;
  private readonly ILetsEncryptService _letsEncryptService;
  private readonly ICacheService _cacheService;
  private readonly IAgentService _agentService;

  private readonly string _acmePath;

  public CertsFlowService(
    IOptions<Configuration> appSettings,
    ILogger<CertsFlowService> logger,
    ILetsEncryptService letsEncryptService,
    ICacheService cashService,
    IAgentService agentService
  ) {
    _appSettings = appSettings.Value;
    _logger = logger;
    _letsEncryptService = letsEncryptService;
    _cacheService = cashService;
    _agentService = agentService;

    _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");
    if (!Directory.Exists(_acmePath))
      Directory.CreateDirectory(_acmePath);
  }

  #region Common methods



  public (string?, IDomainResult) GetTermsOfService(Guid sessionId) {
    var (terms, getTermsResult) = _letsEncryptService.GetTermsOfServiceUri(sessionId);
    if (!getTermsResult.IsSuccess || terms == null)
      return (null, getTermsResult);

    return IDomainResult.Success<string>(terms);
  }

  public async Task<IDomainResult> CompleteChallengesAsync(Guid sessionId) {
    return await _letsEncryptService.CompleteChallenges(sessionId);
  }

  #endregion

  #region Internal methods
  public async Task<(Guid?, IDomainResult)> ConfigureClientAsync(bool isStaging) {
    var sessionId = Guid.NewGuid();

    var result = await _letsEncryptService.ConfigureClient(sessionId, isStaging);
    if (!result.IsSuccess)
      return (null, result);

    return IDomainResult.Success(sessionId);
  }

  public async Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts) {
    RegistrationCache? cache = null;

    if (accountId == null) {
      accountId = Guid.NewGuid();
    }
    else {
      var (loadedCache, loadCaceResutl) = await _cacheService.LoadAccountFromCacheAsync(accountId.Value);
      if (!loadCaceResutl.IsSuccess || loadCaceResutl == null) {
        accountId = Guid.NewGuid();
      }
      else {
        cache = loadedCache;
      }
    }

    var result = await _letsEncryptService.Init(sessionId, accountId.Value, description, contacts, cache);
    return result.IsSuccess ? IDomainResult.Success<Guid>(accountId.Value) : (null, result);
  }

  public async Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType) {
    var (results, newOrderResult) = await _letsEncryptService.NewOrder(sessionId, hostnames, challengeType);
    if (!newOrderResult.IsSuccess || results == null)
      return (null, newOrderResult);

    var challenges = new List<string>();
    foreach (var result in results) {
      string[] splitToken = result.Value.Split('.');
      File.WriteAllText(Path.Combine(_acmePath, splitToken[0]), result.Value);
      challenges.Add(splitToken[0]);
    }

    return IDomainResult.Success(challenges);
  }

  public async Task<IDomainResult> GetCertificatesAsync(Guid sessionId, string[] hostnames) {
    foreach (var subject in hostnames) {
      var result = await _letsEncryptService.GetCertificate(sessionId, subject);
      if (!result.IsSuccess)
        return result;

      Thread.Sleep(1000);
    }

    // Persist the cache
    var (cache, getCacheResult) = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!getCacheResult.IsSuccess || cache == null)
      return getCacheResult;

    var saveResult = await _cacheService.SaveToCacheAsync(cache.AccountId, cache);
    if (!saveResult.IsSuccess)
      return saveResult;

    return IDomainResult.Success();
  }

  public async Task<IDomainResult> GetOrderAsync(Guid sessionId, string[] hostnames) {
    return await _letsEncryptService.GetOrder(sessionId, hostnames);
  }

  public async Task<(Dictionary<string, string>?, IDomainResult)> ApplyCertificatesAsync(Guid sessionId, string[] hostnames) {

    var (cache, getCacheResult) = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!getCacheResult.IsSuccess || cache?.CachedCerts == null)
      return (null, getCacheResult);


    var results = new Dictionary<string, string>();
    foreach (var hostname in hostnames) {
      CertificateCache? cert;
      if (cache.TryGetCachedCertificate(hostname, out cert)) {
        var content = $"{cert.Cert}\n{cert.PrivatePem}";
        results.Add(hostname, content);
      }
    }

    // Send the certificates to the  via agent
    var uploadResult = await _agentService.UploadCerts(results);
    if (!uploadResult.IsSuccess)
      return (null, uploadResult);

    var reloadResult = await _agentService.ReloadService(_appSettings.Agent.ServiceToReload);
    if (!reloadResult.IsSuccess)
      return (null, reloadResult);

    return IDomainResult.Success(results);
  }

  #endregion

  #region REST methods
  public Task<(Guid?, IDomainResult)> ConfigureClientAsync(ConfigureClientRequest requestData) =>
    ConfigureClientAsync(requestData.IsStaging);

  public Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData) =>
    InitAsync(sessionId, accountId, requestData.Description, requestData.Contacts);

  public Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, NewOrderRequest requestData) =>
    NewOrderAsync(sessionId, requestData.Hostnames, requestData.ChallengeType);

  public Task<IDomainResult> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData) =>
  GetCertificatesAsync(sessionId, requestData.Hostnames);

  public Task<IDomainResult> GetOrderAsync(Guid sessionId, GetOrderRequest requestData) =>
    GetOrderAsync(sessionId, requestData.Hostnames);

  public Task<(Dictionary<string, string>?, IDomainResult)> ApplyCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData) =>
    ApplyCertificatesAsync(sessionId, requestData.Hostnames);

  #endregion

  #region Acme Challenge REST methods

  public (string?, IDomainResult) AcmeChallenge(string fileName) {
    DeleteExporedChallenges();

    var fileContent = File.ReadAllText(Path.Combine(_acmePath, fileName));
    if (fileContent == null)
      return IDomainResult.NotFound<string?>();

    return IDomainResult.Success(fileContent);
  }

  private void DeleteExporedChallenges() {
    var currentDate = DateTime.Now;

    foreach (var file in Directory.GetFiles(_acmePath)) {
      try {
        var creationTime = File.GetCreationTime(file);

        // Calculate the time difference
        var timeDifference = currentDate - creationTime;

        // If the file is older than 1 day, delete it
        if (timeDifference.TotalDays > 1) {


          File.Delete(file);
          _logger.LogInformation($"Deleted file: {file}");
        }
      }
      catch (Exception ex) {
        _logger.LogWarning(ex, "File cannot be deleted");
      }
    }
  }

  #endregion
}
