using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.Engine.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// ACME / Let's Encrypt certificate flows (domain layer). Engine returns <see cref="MaksIT.Results.Result"/> / <see cref="MaksIT.Results.Result{T}"/>; the Web API materializes them to HTTP.
/// </summary>
public interface ICertsFlowDomainService {

  #region Terms of service
  Result<string?> GetTermsOfService(Guid sessionId);
  #endregion

  #region Session, orders, and certificates
  Task<Result> CompleteChallengesAsync(Guid sessionId);
  Task<Result<Guid?>> ConfigureClientAsync(bool isStaging);
  Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames);
  #endregion

  #region Deploy and revoke
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId);
  Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames);
  #endregion

  #region Full orchestration
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
  #endregion

  #region HTTP-01 challenge
  Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default);
  #endregion
}

/// <summary>
/// Certs-only domain service for ACME flows (no 2FA or entity scopes; same layering as <see cref="ApiKeyDomainService"/> / <see cref="IdentityDomainService"/>).
/// </summary>
public class CertsFlowDomainService : ICertsFlowDomainService {

  private static readonly TimeSpan AcmeWriterLeaseTtl = TimeSpan.FromMinutes(20);

  private readonly ILogger<CertsFlowDomainService> _logger;
  private readonly HttpClient _httpClient;
  private readonly ILetsEncryptService _letsEncryptService;
  private readonly IRegistrationCachePersistanceService _registrationCache;
  private readonly IAgentDeploymentService _agentDeployment;
  private readonly ICertsFlowEngineConfiguration _config;
  private readonly IAcmeHttpChallengePersistenceService _httpChallenges;
  private readonly IRuntimeLeaseService _runtimeLease;
  private readonly IRuntimeInstanceId _runtimeInstance;
  private readonly string _acmePath;

  public CertsFlowDomainService(
    ILogger<CertsFlowDomainService> logger,
    HttpClient httpClient,
    ILetsEncryptService letsEncryptService,
    IRegistrationCachePersistanceService registrationCache,
    IAgentDeploymentService agentDeployment,
    ICertsFlowEngineConfiguration config,
    IAcmeHttpChallengePersistenceService httpChallenges,
    IRuntimeLeaseService runtimeLease,
    IRuntimeInstanceId runtimeInstance) {
    _logger = logger;
    _httpClient = httpClient;
    _letsEncryptService = letsEncryptService;
    _registrationCache = registrationCache;
    _agentDeployment = agentDeployment;
    _config = config;
    _httpChallenges = httpChallenges;
    _runtimeLease = runtimeLease;
    _runtimeInstance = runtimeInstance;
    _acmePath = config.AcmeFolder;
  }

  #region Terms of service

  public Result<string?> GetTermsOfService(Guid sessionId) {
    var result = _letsEncryptService.GetTermsOfServiceUri(sessionId);
    if (!result.IsSuccess || result.Value == null)
      return result;

    var termsOfServiceUrl = result.Value;

    try {
      var fileName = Path.GetFileName(new Uri(termsOfServiceUrl).LocalPath);
      var termsOfServicePdfPath = Path.Combine(_config.DataFolder, fileName);
      foreach (var file in Directory.GetFiles(_config.DataFolder, "*.pdf")) {
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
      }
      else {
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

  #endregion

  #region Session, orders, and certificates

  public async Task<Result> CompleteChallengesAsync(Guid sessionId) =>
    await _letsEncryptService.CompleteChallenges(sessionId);

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
      var cacheResult = await _registrationCache.LoadAsync(accountId.Value);
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
    var holder = _runtimeInstance.InstanceId;
    var acquired = await _runtimeLease.TryAcquireAsync(RuntimeLeaseNames.AcmeWriter, holder, AcmeWriterLeaseTtl, CancellationToken.None);
    if (!acquired.IsSuccess)
      return Result<List<string>?>.InternalServerError(null, acquired.Messages?.ToArray() ?? ["ACME writer lease check failed."]);
    if (!acquired.Value) {
      _logger.LogWarning("ACME writer lease busy; another instance holds {Lease}.", RuntimeLeaseNames.AcmeWriter);
      return Result<List<string>?>.TooManyRequests(null, "Another CertsUI instance is performing ACME order work. Retry shortly.");
    }

    try {
      var orderResult = await _letsEncryptService.NewOrder(sessionId, hostnames, challengeType);
      if (!orderResult.IsSuccess || orderResult.Value == null)
        return orderResult.ToResultOfType<List<string>?>(_ => null);
      var challenges = new List<string>();
      foreach (var kvp in orderResult.Value) {
        var parts = kvp.Value.Split('.');
        if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
          return Result<List<string>?>.InternalServerError(null, "Invalid challenge token from ACME order.");
        var fileName = parts[0];
        var upsert = await _httpChallenges.UpsertAsync(fileName, kvp.Value, CancellationToken.None);
        if (!upsert.IsSuccess)
          return upsert.ToResultOfType<List<string>?>(null);
        challenges.Add(fileName);
      }

      return Result<List<string>?>.Ok(challenges);
    }
    finally {
      var released = await _runtimeLease.ReleaseAsync(RuntimeLeaseNames.AcmeWriter, holder, CancellationToken.None);
      if (!released.IsSuccess)
        _logger.LogWarning("Failed to release ACME writer lease for {Holder}: {Messages}", holder, string.Join("; ", released.Messages ?? []));
    }
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
    var saveResult = await _registrationCache.SaveAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      return saveResult;
    return Result.Ok();
  }

  public async Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames) =>
    await _letsEncryptService.GetOrder(sessionId, hostnames);

  #endregion

  #region Deploy and revoke

  public async Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId) {
    var cacheResult = await _registrationCache.LoadAsync(accountId);
    if (!cacheResult.IsSuccess || cacheResult.Value?.CachedCerts == null)
      return cacheResult.ToResultOfType<Dictionary<string, string>?>(_ => null);
    var cache = cacheResult.Value;
    var results = cache.GetCertsPemPerHostname();
    if (cache.IsDisabled)
      return Result<Dictionary<string, string>?>.BadRequest(null, $"Account {accountId} is disabled");
    if (cache.IsStaging)
      return Result<Dictionary<string, string>?>.UnprocessableEntity(null, $"Found certs for {string.Join(',', results.Keys)} (staging environment)");
    var uploadResult = await _agentDeployment.UploadCerts(results);
    if (!uploadResult.IsSuccess)
      return uploadResult.ToResultOfType<Dictionary<string, string>?>(default);
    var reloadResult = await _agentDeployment.ReloadService(_config.AgentServiceToReload);
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
    var saveResult = await _registrationCache.SaveAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      return saveResult;
    return Result.Ok();
  }

  #endregion

  #region Full orchestration

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

    if (!challengesResult.IsSuccess) {
      await TryPersistRegistrationCacheFromSessionAsync(sessionId);
      return challengesResult.ToResultOfType<Guid?>(_ => null);
    }

    if (challengesResult.Value?.Count > 0) {
      var challengeResult = await CompleteChallengesAsync(sessionId);
      if (!challengeResult.IsSuccess) {
        await TryPersistRegistrationCacheFromSessionAsync(sessionId);
        return challengeResult.ToResultOfType<Guid?>(default);
      }
    }

    var getOrderResult = await GetOrderAsync(sessionId, hostnames);
    if (!getOrderResult.IsSuccess) {
      await TryPersistRegistrationCacheFromSessionAsync(sessionId);
      return getOrderResult.ToResultOfType<Guid?>(default);
    }

    var certsResult = await GetCertificatesAsync(sessionId, hostnames);
    if (!certsResult.IsSuccess) {
      await TryPersistRegistrationCacheFromSessionAsync(sessionId);
      return certsResult.ToResultOfType<Guid?>(default);
    }

    if (!isStaging) {
      var applyCertsResult = await ApplyCertificatesAsync(accountId.Value);
      if (!applyCertsResult.IsSuccess) {
        await TryPersistRegistrationCacheFromSessionAsync(sessionId);
        return applyCertsResult.ToResultOfType<Guid?>(_ => null);
      }
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

  #region HTTP-01 challenge

  public async Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(fileName))
      return Result<string?>.BadRequest(null, "fileName is required.");

    var fromDb = await _httpChallenges.GetTokenValueAsync(fileName, cancellationToken).ConfigureAwait(false);
    if (fromDb.IsSuccess && !string.IsNullOrEmpty(fromDb.Value)) {
      Directory.CreateDirectory(_acmePath);
      var path = Path.Combine(_acmePath, fileName);
      await File.WriteAllTextAsync(path, fromDb.Value!, cancellationToken).ConfigureAwait(false);
      return Result<string?>.Ok(fromDb.Value);
    }

    var legacyPath = Path.Combine(_acmePath, fileName);
    if (File.Exists(legacyPath)) {
      var legacy = await File.ReadAllTextAsync(legacyPath, cancellationToken).ConfigureAwait(false);
      return Result<string?>.Ok(legacy);
    }

    return Result<string?>.NotFound(null, $"Challenge token not found: {fileName}");
  }

  #endregion

  private async Task TryPersistRegistrationCacheFromSessionAsync(Guid sessionId) {
    var cacheResult = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!cacheResult.IsSuccess || cacheResult.Value == null)
      return;

    var saveResult = await _registrationCache.SaveAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      _logger.LogWarning("Could not persist registration cache after ACME flow step for account {AccountId}.", cacheResult.Value.AccountId);
  }

}
