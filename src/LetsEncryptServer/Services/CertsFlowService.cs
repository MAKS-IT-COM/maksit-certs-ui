using Microsoft.Extensions.Options;
using MaksIT.Results;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;
using MaksIT.LetsEncrypt.Services;


namespace MaksIT.LetsEncryptServer.Services;

public interface ICertsFlowService {
  Result<string?> GetTermsOfService(Guid sessionId);
  Task<Result> CompleteChallengesAsync(Guid sessionId);
  Task<Result<Guid?>> ConfigureClientAsync(bool isStaging);
  Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId);
  Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
  Result<string?> AcmeChallenge(string fileName);
}

public class CertsFlowService : ICertsFlowService {
  private readonly Configuration _appSettings;
  private readonly ILogger<CertsFlowService> _logger;
  private readonly HttpClient _httpClient;
  private readonly ILetsEncryptService _letsEncryptService;
  private readonly ICacheService _cacheService;
  private readonly IAgentService _agentService;
  private readonly string _acmePath;

  public CertsFlowService(
    IOptions<Configuration> appSettings,
    ILogger<CertsFlowService> logger,
    HttpClient httpClient,
    ILetsEncryptService letsEncryptService,
    ICacheService cashService,
    IAgentService agentService
  ) {
    _appSettings = appSettings.Value;
    _logger = logger;
    _httpClient = httpClient;
    _letsEncryptService = letsEncryptService;
    _cacheService = cashService;
    _agentService = agentService;
    _acmePath = _appSettings.AcmeFolder;
  }

  public Result<string?> GetTermsOfService(Guid sessionId) {
    var result = _letsEncryptService.GetTermsOfServiceUri(sessionId);
    if (!result.IsSuccess || result.Value == null)
      return result;

    var termsOfServiceUrl = result.Value;

    try {
        var fileName = Path.GetFileName(new Uri(termsOfServiceUrl).LocalPath);

        var termsOfServicePdfPath = Path.Combine(_appSettings.DataFolder, fileName);

        // Clean up old PDF files except the current one
        foreach (var file in Directory.GetFiles(_appSettings.DataFolder, "*.pdf")) {
            if (!string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase)) {
                try {
                  File.Delete(file);
                }
                catch { /* ignore */ }
            }
        }

        byte[] pdfBytes;
        if (File.Exists(termsOfServicePdfPath)) {
            pdfBytes = File.ReadAllBytes(termsOfServicePdfPath);
        } else {
            pdfBytes = _httpClient.GetByteArrayAsync(termsOfServiceUrl).GetAwaiter().GetResult();
            File.WriteAllBytes(termsOfServicePdfPath, pdfBytes);
        }
        var base64 = Convert.ToBase64String(pdfBytes);
        return Result<string?>.Ok(base64);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to download, cache, or convert Terms of Service PDF");
      return Result<string?>.InternalServerError(null, $"Failed to download, cache, or convert Terms of Service PDF: {ex.Message}");
    }
  }

  public async Task<Result> CompleteChallengesAsync(Guid sessionId) {
    return await _letsEncryptService.CompleteChallenges(sessionId);
  }

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
    }
    else {
      var cacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId.Value);

      if (!cacheResult.IsSuccess || cacheResult.Value == null) {
        accountId = Guid.NewGuid();
      }
      else {
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

  public async Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId) {
    var cacheResult = await _cacheService.LoadAccountFromCacheAsync(accountId);
    if (!cacheResult.IsSuccess || cacheResult.Value?.CachedCerts == null)
      return cacheResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

    var cache = cacheResult.Value;
    var results = cache.GetCertsPemPerHostname();


    if (cache.IsDisabled)
      return Result<Dictionary<string, string>?>.BadRequest(null, $"Account {accountId} is disabled");

    if (cache.IsStaging)
      return Result<Dictionary<string, string>?>.UnprocessableEntity(null, $"Found certs for {string.Join(',', results.Keys)} (staging environment)");

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
    if (!initResult.IsSuccess || initResult.Value == null)
      return initResult.ToResultOfType<Guid?>(_ => null);

    if (accountId == null)
      accountId = initResult.Value;

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

    if (!isStaging) {
      var applyCertsResult = await ApplyCertificatesAsync(accountId.Value);
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

  public Result<string?> AcmeChallenge(string fileName) {
    DeleteExporedChallenges();

    var challengePath = Path.Combine(_acmePath, fileName);
    if (!File.Exists(challengePath))
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
}
