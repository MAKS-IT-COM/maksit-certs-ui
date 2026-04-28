using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Dto.Certs;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.Engine.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace MaksIT.CertsUI.Engine.DomainServices;

/// <summary>
/// ACME / Let's Encrypt certificate flows (domain layer). Engine returns <see cref="MaksIT.Results.Result"/> / <see cref="MaksIT.Results.Result{T}"/>; the Web API materializes them to HTTP.
/// </summary>
public interface ICertsFlowDomainService {

  #region Terms of service
  Task<Result<string?>> GetTermsOfServiceAsync(Guid sessionId);
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
  private readonly ITermsOfServiceCachePersistenceService _termsOfServiceCache;
  private readonly IAcmeHttpChallengePersistenceService _httpChallenges;
  private readonly IRuntimeLeaseService _runtimeLease;
  private readonly IRuntimeInstanceId _runtimeInstance;

  public CertsFlowDomainService(
    ILogger<CertsFlowDomainService> logger,
    HttpClient httpClient,
    ILetsEncryptService letsEncryptService,
    IRegistrationCachePersistanceService registrationCache,
    IAgentDeploymentService agentDeployment,
    ICertsFlowEngineConfiguration config,
    ITermsOfServiceCachePersistenceService termsOfServiceCache,
    IAcmeHttpChallengePersistenceService httpChallenges,
    IRuntimeLeaseService runtimeLease,
    IRuntimeInstanceId runtimeInstance) {
    _logger = logger;
    _httpClient = httpClient;
    _letsEncryptService = letsEncryptService;
    _registrationCache = registrationCache;
    _agentDeployment = agentDeployment;
    _config = config;
    _termsOfServiceCache = termsOfServiceCache;
    _httpChallenges = httpChallenges;
    _runtimeLease = runtimeLease;
    _runtimeInstance = runtimeInstance;
  }

  #region Terms of service

  public async Task<Result<string?>> GetTermsOfServiceAsync(Guid sessionId) {
    var termsUriResult = await _letsEncryptService.GetTermsOfServiceUriAsync(sessionId, CancellationToken.None).ConfigureAwait(false);
    if (!termsUriResult.IsSuccess || termsUriResult.Value == null)
      return termsUriResult;

    return await GetOrFetchTermsOfServicePdfBase64Async(termsUriResult.Value);
  }

  private async Task<Result<string?>> GetOrFetchTermsOfServicePdfBase64Async(string termsOfServiceUrl) {
    try {
      var cachedResult = await _termsOfServiceCache.GetByUrlAsync(termsOfServiceUrl);
      if (cachedResult.IsSuccess && cachedResult.Value != null && cachedResult.Value.ExpiresAtUtc > DateTimeOffset.UtcNow)
        return Result<string?>.Ok(Convert.ToBase64String(cachedResult.Value.ContentBytes));

      using var request = new HttpRequestMessage(HttpMethod.Get, termsOfServiceUrl);
      if (cachedResult.IsSuccess && cachedResult.Value != null) {
        if (!string.IsNullOrWhiteSpace(cachedResult.Value.ETag))
          request.Headers.TryAddWithoutValidation("If-None-Match", cachedResult.Value.ETag);
        if (cachedResult.Value.LastModifiedUtc.HasValue)
          request.Headers.IfModifiedSince = cachedResult.Value.LastModifiedUtc.Value;
      }

      var response = await _httpClient.SendAsync(request);
      if (response.StatusCode == HttpStatusCode.NotModified && cachedResult.IsSuccess && cachedResult.Value != null) {
        var now = DateTimeOffset.UtcNow;
        var notModifiedEntry = cachedResult.Value;
        notModifiedEntry.FetchedAtUtc = now;
        notModifiedEntry.ExpiresAtUtc = GetExpiry(now, response.Headers.CacheControl, response.Content.Headers.Expires);
        var refreshResult = await _termsOfServiceCache.UpsertAsync(notModifiedEntry);
        if (!refreshResult.IsSuccess)
          return refreshResult.ToResultOfType<string?>(null);
        return Result<string?>.Ok(Convert.ToBase64String(notModifiedEntry.ContentBytes));
      }

      if (!response.IsSuccessStatusCode)
        return Result<string?>.InternalServerError(null, $"Failed to download Terms of Service PDF. Status: {(int)response.StatusCode} {response.ReasonPhrase}");

      var bytes = await response.Content.ReadAsByteArrayAsync();
      if (bytes.Length == 0)
        return Result<string?>.InternalServerError(null, "Downloaded Terms of Service PDF is empty.");

      var fetchedAt = DateTimeOffset.UtcNow;
      var cacheEntry = new TermsOfServiceCacheDto {
        Url = termsOfServiceUrl,
        UrlHashHex = ComputeSha256Hex(termsOfServiceUrl),
        ETag = response.Headers.ETag?.Tag,
        LastModifiedUtc = response.Content.Headers.LastModified,
        ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf",
        ContentBytes = bytes,
        FetchedAtUtc = fetchedAt,
        ExpiresAtUtc = GetExpiry(fetchedAt, response.Headers.CacheControl, response.Content.Headers.Expires)
      };

      var upsertResult = await _termsOfServiceCache.UpsertAsync(cacheEntry);
      if (!upsertResult.IsSuccess)
        return upsertResult.ToResultOfType<string?>(null);

      return Result<string?>.Ok(Convert.ToBase64String(bytes));
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to fetch or cache Terms of Service PDF");
      return Result<string?>.InternalServerError(null, $"Failed to fetch or cache Terms of Service PDF: {ex.Message}");
    }
  }

  private static string ComputeSha256Hex(string text) {
    var bytes = Encoding.UTF8.GetBytes(text);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }

  private static DateTimeOffset GetExpiry(DateTimeOffset now, CacheControlHeaderValue? cacheControl, DateTimeOffset? expiresHeader) {
    if (cacheControl?.MaxAge is TimeSpan maxAge && maxAge > TimeSpan.Zero)
      return now.Add(maxAge);
    if (expiresHeader.HasValue && expiresHeader.Value > now)
      return expiresHeader.Value;
    return now.AddHours(24);
  }

  #endregion

  #region Session, orders, and certificates

  public async Task<Result> CompleteChallengesAsync(Guid sessionId) {
    return await _letsEncryptService.CompleteChallenges(sessionId, CancellationToken.None).ConfigureAwait(false);
  }

  public async Task<Result<Guid?>> ConfigureClientAsync(bool isStaging) {
    var sessionId = Guid.NewGuid();
    var result = await _letsEncryptService.ConfigureClient(sessionId, isStaging, CancellationToken.None).ConfigureAwait(false);
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
    var result = await _letsEncryptService.Init(sessionId, accountId.Value, description, contacts, cache, CancellationToken.None).ConfigureAwait(false);
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
      var orderResult = await _letsEncryptService.NewOrder(sessionId, hostnames, challengeType, CancellationToken.None).ConfigureAwait(false);
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
      var result = await _letsEncryptService.GetCertificate(sessionId, subject, CancellationToken.None).ConfigureAwait(false);
      if (!result.IsSuccess)
        return result;
      Thread.Sleep(1000);
    }
    var cacheResult = await _letsEncryptService.GetRegistrationCacheAsync(sessionId, CancellationToken.None).ConfigureAwait(false);
    if (!cacheResult.IsSuccess || cacheResult.Value == null)
      return cacheResult;
    var saveResult = await _registrationCache.SaveAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      return saveResult;
    return Result.Ok();
  }

  public async Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames) {
    return await _letsEncryptService.GetOrder(sessionId, hostnames, CancellationToken.None).ConfigureAwait(false);
  }

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
      var result = await _letsEncryptService.RevokeCertificate(sessionId, hostname, RevokeReason.Unspecified, CancellationToken.None).ConfigureAwait(false);
      if (!result.IsSuccess)
        return result;
    }
    var cacheResult = await _letsEncryptService.GetRegistrationCacheAsync(sessionId, CancellationToken.None).ConfigureAwait(false);
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
    if (fromDb.IsSuccess && !string.IsNullOrEmpty(fromDb.Value))
      return Result<string?>.Ok(fromDb.Value);

    return Result<string?>.NotFound(null, $"Challenge token not found: {fileName}");
  }

  #endregion

  private async Task TryPersistRegistrationCacheFromSessionAsync(Guid sessionId) {
    var cacheResult = await _letsEncryptService.GetRegistrationCacheAsync(sessionId, CancellationToken.None).ConfigureAwait(false);
    if (!cacheResult.IsSuccess || cacheResult.Value == null)
      return;

    var saveResult = await _registrationCache.SaveAsync(cacheResult.Value.AccountId, cacheResult.Value);
    if (!saveResult.IsSuccess)
      _logger.LogWarning("Could not persist registration cache after ACME flow step for account {AccountId}.", cacheResult.Value.AccountId);
  }

}
