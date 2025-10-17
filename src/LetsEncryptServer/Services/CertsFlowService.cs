using Microsoft.Extensions.Options;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Services;
using MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;
using MaksIT.Results;


namespace MaksIT.LetsEncryptServer.Services;

public interface ICertsCommonService {
  Result<string?> GetTermsOfService(Guid sessionId);
  Task<Result> CompleteChallengesAsync(Guid sessionId);
}

public interface ICertsInternalService : ICertsCommonService {
  Task<Result<Guid?>> ConfigureClientAsync(bool isStaging);
  Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
}

public interface ICertsRestService : ICertsCommonService {
  Task<Result<Guid?>> ConfigureClientAsync(ConfigureClientRequest requestData);
  Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData);
  Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, NewOrderRequest requestData);
  Task<Result> GetOrderAsync(Guid sessionId, GetOrderRequest requestData);
  Task<Result> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData);
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData);
  Task<Result> RevokeCertificatesAsync(Guid sessionId, RevokeCertificatesRequest requestData);
}

public interface ICertsRestChallengeService {
  Result<string?> AcmeChallenge(string fileName);
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
    _acmePath = _appSettings.AcmeFolder;
  }

  #region Common methods
  public Result<string?> GetTermsOfService(Guid sessionId) {
    var result = _letsEncryptService.GetTermsOfServiceUri(sessionId);
    return result;
  }

  public async Task<Result> CompleteChallengesAsync(Guid sessionId) {
    return await _letsEncryptService.CompleteChallenges(sessionId);
  }
  #endregion

  #region Internal methods
  public async Task<Result<Guid?>> ConfigureClientAsync(bool isStaging) {
    var sessionId = Guid.NewGuid();
    var result = await _letsEncryptService.ConfigureClient(sessionId, isStaging);
    if (!result.IsSuccess)
      return result.ToResultOfType<Guid?>(default);
    return Result<Guid?>.Ok(sessionId);
  }

  public async Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts) {
    RegistrationCache? cache = null;
    if (accountId == null) {
      accountId = Guid.NewGuid();
    } else {
      var cacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId.Value);
      if (!cacheResult.IsSuccess || cacheResult.Value == null) {
        accountId = Guid.NewGuid();
      } else {
        cache = cacheResult.Value;
      }
    }
    var result = await _letsEncryptService.Init(sessionId, accountId.Value, description, contacts, cache);
    if (!result.IsSuccess)
      return result.ToResultOfType<Guid?>(default);
    return Result<Guid?>.Ok(accountId.Value);
  }

  public async Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType) {
    var orderResult = await _letsEncryptService.NewOrder(sessionId, hostnames, challengeType);
    if (!orderResult.IsSuccess || orderResult.Value == null)
      return orderResult.ToResultOfType<List<string>?>(_ => null);
    var challenges = new List<string>();
    foreach (var kvp in orderResult.Value) {
      string[] splitToken = kvp.Value.Split('.');
      File.WriteAllText(Path.Combine(_acmePath, splitToken[0]), kvp.Value);
      challenges.Add(splitToken[0]);
    }
    return Result<List<string>?>.Ok(challenges);
  }

  public async Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames) {
    foreach (var subject in hostnames) {
      var result = await _letsEncryptService.GetCertificate(sessionId, subject);
      if (!result.IsSuccess)
        return result;
      Thread.Sleep(1000);
    }
    var cacheResult = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!cacheResult.IsSuccess || cacheResult.Value == null)
      return cacheResult;
    var saveResult = await _cacheService.SaveToCacheAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      return saveResult;
    return Result.Ok();
  }

  public async Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames) {
    return await _letsEncryptService.GetOrder(sessionId, hostnames);
  }

  public async Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid sessionId, string[] hostnames) {
    var cacheResult = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!cacheResult.IsSuccess || cacheResult.Value?.CachedCerts == null)
      return cacheResult.ToResultOfType<Dictionary<string, string>?>(_ => null);
    var results = new Dictionary<string, string>();
    foreach (var hostname in hostnames) {
      CertificateCache? cert;
      if (cacheResult.Value.TryGetCachedCertificate(hostname, out cert)) {
        var content = $"{cert.Cert}\n{cert.PrivatePem}";
        results.Add(hostname, content);
      }
    }
    var uploadResult = await _agentService.UploadCerts(results);
    if (!uploadResult.IsSuccess)
      return uploadResult.ToResultOfType<Dictionary<string, string>?>(default);
    var reloadResult = await _agentService.ReloadService(_appSettings.Agent.ServiceToReload);
    if (!reloadResult.IsSuccess)
      return reloadResult.ToResultOfType<Dictionary<string, string>?>(default);
    return Result<Dictionary<string, string>?>.Ok(results);
  }

  public async Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames) {
    foreach (var hostname in hostnames) {
      var result = await _letsEncryptService.RevokeCertificate(sessionId, hostname, RevokeReason.Unspecified);
      if (!result.IsSuccess)
        return result;
    }
    var cacheResult = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!cacheResult.IsSuccess || cacheResult.Value == null)
      return cacheResult;
    var saveResult = await _cacheService.SaveToCacheAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      return saveResult;
    return Result.Ok();
  }

  public async Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames) {
    var sessionResult = await ConfigureClientAsync(isStaging);
    if (!sessionResult.IsSuccess || sessionResult.Value == null)
      return sessionResult;

    var sessionId = sessionResult.Value.Value;

    var initResult = await InitAsync(sessionId, accountId, description, contacts);
    if (!initResult.IsSuccess)
      return initResult.ToResultOfType<Guid?>(_ => null);

    var challengesResult = await NewOrderAsync(sessionId, hostnames, challengeType);
    if (!challengesResult.IsSuccess)
      return challengesResult.ToResultOfType<Guid?>(_ => null);

    if (challengesResult.Value?.Count > 0) {
      var challengeResult = await CompleteChallengesAsync(sessionId);
      if (!challengeResult.IsSuccess)
        return challengeResult.ToResultOfType<Guid?>(default);
    }
    var getOrderResult = await GetOrderAsync(sessionId, hostnames);
    if (!getOrderResult.IsSuccess)
      return getOrderResult.ToResultOfType<Guid?>(default);

    var certsResult = await GetCertificatesAsync(sessionId, hostnames);
    if (!certsResult.IsSuccess)
      return certsResult.ToResultOfType<Guid?>(default);

    // Bypass applying certificates in staging mode
    if (!isStaging) { 
        var applyCertsResult = await ApplyCertificatesAsync(sessionId, hostnames);
        if (!applyCertsResult.IsSuccess)
            return applyCertsResult.ToResultOfType<Guid?>(_ => null);
    }

    return Result<Guid?>.Ok(initResult.Value);
  }

  public async Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames) {
    var sessionResult = await ConfigureClientAsync(isStaging);
    if (!sessionResult.IsSuccess || sessionResult.Value == null)
      return sessionResult;

    var sessionId = sessionResult.Value.Value;

    var initResult = await InitAsync(sessionId, accountId, description, contacts);
    if (!initResult.IsSuccess)
      return initResult;
    var revokeResult = await RevokeCertificatesAsync(sessionId, hostnames);
    if (!revokeResult.IsSuccess)
      return revokeResult;
    return Result.Ok();
  }
  #endregion

  #region REST methods
  public async Task<Result<Guid?>> ConfigureClientAsync(ConfigureClientRequest requestData) {
    return await ConfigureClientAsync(requestData.IsStaging);
  }
  public async Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData) {
    return await InitAsync(sessionId, accountId, requestData.Description, requestData.Contacts);
  }
  public async Task<Result<List<string>>> NewOrderAsync(Guid sessionId, NewOrderRequest requestData) {
    return await NewOrderAsync(sessionId, requestData.Hostnames, requestData.ChallengeType);
  }
  public async Task<Result> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData) {
    return await GetCertificatesAsync(sessionId, requestData.Hostnames);
  }
  public async Task<Result> GetOrderAsync(Guid sessionId, GetOrderRequest requestData) {
    return await GetOrderAsync(sessionId, requestData.Hostnames);
  }

  public async Task<Result<Dictionary<string, string>>> ApplyCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData) =>
    await ApplyCertificatesAsync(sessionId, requestData.Hostnames);
  public async Task<Result> RevokeCertificatesAsync(Guid sessionId, RevokeCertificatesRequest requestData) =>
    await RevokeCertificatesAsync(sessionId, requestData.Hostnames);
  #endregion

  #region Acme Challenge REST methods
  public Result<string?> AcmeChallenge(string fileName) {
    DeleteExporedChallenges();
    var challengePath = Path.Combine(_acmePath, fileName);
    if(!File.Exists(challengePath))
      return Result<string?>.NotFound(null);
    var fileContent = File.ReadAllText(Path.Combine(_acmePath, fileName));
    return Result<string?>.Ok(fileContent);
  }

  private void DeleteExporedChallenges() {
    var currentDate = DateTime.Now;
    foreach (var file in Directory.GetFiles(_acmePath)) {
      try {
        var creationTime = File.GetCreationTime(file);
        var timeDifference = currentDate - creationTime;
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
