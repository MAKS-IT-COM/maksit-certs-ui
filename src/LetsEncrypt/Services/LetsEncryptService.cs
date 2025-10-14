/**
 * https://datatracker.ietf.org/doc/html/rfc8555
 * https://datatracker.ietf.org/doc/html/draft-ietf-acme-acme-12
 */


using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http.Headers;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using DomainResults.Common;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Models.Interfaces;
using MaksIT.LetsEncrypt.Models.Requests;
using MaksIT.LetsEncrypt.Entities.Jws;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;

namespace MaksIT.LetsEncrypt.Services;

public interface ILetsEncryptService {
  Task<IDomainResult> ConfigureClient(Guid sessionId, bool isStaging);
  Task<IDomainResult> Init(Guid sessionId,Guid accountId, string description, string[] contacts, RegistrationCache? registrationCache);
  (RegistrationCache?, IDomainResult) GetRegistrationCache(Guid sessionId);
  (string?, IDomainResult) GetTermsOfServiceUri(Guid sessionId);
  Task<(Dictionary<string, string>?, IDomainResult)> NewOrder(Guid sessionId, string[] hostnames, string challengeType);
  Task<IDomainResult> CompleteChallenges(Guid sessionId);
  Task<IDomainResult> GetOrder(Guid sessionId, string[] hostnames);
  Task<IDomainResult> GetCertificate(Guid sessionId, string subject);
  Task<IDomainResult> RevokeCertificate(Guid sessionId, string subject, RevokeReason reason);
}

public class LetsEncryptService : ILetsEncryptService {
  private const string DnsType = "dns";
  private const string DirectoryEndpoint = "directory";
  private const string ReplayNonceHeader = "Replay-Nonce";

  private readonly ILogger<LetsEncryptService> _logger;
  private readonly LetsEncryptConfiguration _appSettings;
  private readonly HttpClient _httpClient;
  private readonly IMemoryCache _memoryCache;

  public LetsEncryptService(
      ILogger<LetsEncryptService> logger,
      LetsEncryptConfiguration appSettings,
      HttpClient httpClient,
      IMemoryCache cache
   ) {
    _logger = logger;
    _appSettings = appSettings;
    _httpClient = httpClient;
    _memoryCache = cache;
  }

  private State GetOrCreateState(Guid sessionId) {
    if (!_memoryCache.TryGetValue(sessionId, out State? state) || state == null) {
      state = new State();
      _memoryCache.Set(sessionId, state, TimeSpan.FromHours(1));
    }
    return state;
  }

  // Helper: Send ACME request and process response
  private async Task<SendResult<T>> SendAcmeRequest<T>(HttpRequestMessage request, State state, HttpMethod method) {
    var response = await _httpClient.SendAsync(request);
    UpdateStateNonceIfNeeded(response, state, method);
    var responseText = await response.Content.ReadAsStringAsync();
    HandleProblemResponseAsync(response, responseText);
    return ProcessResponseContent<T>(response, responseText);
  }

  // Helper: Poll challenge status until valid or timeout
  private async Task<IDomainResult> PollChallengeStatus(Guid sessionId, AuthorizationChallengeChallenge challenge, State state) {
    if (challenge?.Url == null) return IDomainResult.Failed("Challenge URL is null");
    var start = DateTime.UtcNow;
    while (true) {
      var pollRequest = new HttpRequestMessage(HttpMethod.Post, challenge.Url);
      await HandleNonceAsync(sessionId, challenge.Url, state);
      var pollJson = EncodeMessage(true, null, state, new JwsHeader {
        Url = challenge.Url,
        Nonce = state.Nonce
      });
      PrepareRequestContent(pollRequest, pollJson, HttpMethod.Post);
      var pollResponse = await _httpClient.SendAsync(pollRequest);
      UpdateStateNonceIfNeeded(pollResponse, state, HttpMethod.Post);
      var pollResponseText = await pollResponse.Content.ReadAsStringAsync();
      HandleProblemResponseAsync(pollResponse, pollResponseText);
      var authChallenge = ProcessResponseContent<AuthorizationChallengeResponse>(pollResponse, pollResponseText);
      if (authChallenge.Result?.Status != "pending")
        return authChallenge.Result?.Status == "valid" ? IDomainResult.Success() : IDomainResult.Failed();
      if ((DateTime.UtcNow - start).Seconds > 120)
        return IDomainResult.Failed("Timeout");
      await Task.Delay(1000);
    }
  }

  #region ConfigureClient
  public async Task<IDomainResult> ConfigureClient(Guid sessionId, bool isStaging) {
    try {
      var state = GetOrCreateState(sessionId);
      state.IsStaging = isStaging;
      _httpClient.BaseAddress ??= new Uri(isStaging ? _appSettings.Staging : _appSettings.Production);
      if (state.Directory == null) {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(DirectoryEndpoint, UriKind.Relative));
        await HandleNonceAsync(sessionId, new Uri(DirectoryEndpoint, UriKind.Relative), state);
        var directory = await SendAcmeRequest<AcmeDirectory>(request, state, HttpMethod.Get);
        state.Directory = directory.Result ?? throw new InvalidOperationException("Directory response is null");
      }
      return IDomainResult.Success();
    } catch (Exception ex) {
      const string message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }
  #endregion

  #region Init
  public async Task<IDomainResult> Init(Guid sessionId, Guid accountId, string description, string[] contacts, RegistrationCache? cache) {
    if (sessionId == Guid.Empty) {
      _logger.LogError("Invalid sessionId");
      return IDomainResult.Failed();
    }
    if (contacts == null || contacts.Length == 0) {
      _logger.LogError("Contacts are null or empty");
      return IDomainResult.Failed();
    }
    var state = GetOrCreateState(sessionId);
    if (state.Directory == null) {
      _logger.LogError("State directory is null");
      return IDomainResult.Failed();
    }
    _logger.LogInformation($"Executing {nameof(Init)}...");
    try {
      var accountKey = new RSACryptoServiceProvider(4096);
      if (cache != null && cache.AccountKey != null) {
        state.Cache = cache;
        accountKey.ImportCspBlob(cache.AccountKey);
        state.JwsService = new JwsService(accountKey);
        state.JwsService.SetKeyId(cache.Location?.ToString() ?? string.Empty);
      } else {
        state.JwsService = new JwsService(accountKey);
        var letsEncryptOrder = new Account {
          TermsOfServiceAgreed = true,
          Contacts = contacts.Select(contact => $"mailto:{contact}").ToArray()
        };
        var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewAccount);
        await HandleNonceAsync(sessionId, state.Directory.NewAccount, state);
        var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
          Url = state.Directory.NewAccount,
          Nonce = state.Nonce
        });
        PrepareRequestContent(request, json, HttpMethod.Post);
        var result = await SendAcmeRequest<Account>(request, state, HttpMethod.Post);
        state.JwsService.SetKeyId(result.Result?.Location?.ToString() ?? string.Empty);
        if (result.Result?.Status != "valid") {
          _logger.LogError($"Account status is not valid, was: {result.Result?.Status} \r\n {result.ResponseText}");
          return IDomainResult.Failed();
        }
        state.Cache = new RegistrationCache {
          AccountId = accountId,
          Description = description,
          Contacts = contacts,
          IsStaging = state.IsStaging,
          Location = result.Result.Location,
          AccountKey = accountKey.ExportCspBlob(true),
          Id = result.Result.Id ?? string.Empty,
          Key = result.Result.Key
        };
      }
      return IDomainResult.Success();
    } catch (Exception ex) {
      const string message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }
  #endregion

  public (RegistrationCache?, IDomainResult) GetRegistrationCache(Guid sessionId) {
    var state = GetOrCreateState(sessionId);
    if(state?.Cache == null)
      return IDomainResult.Failed<RegistrationCache?>();
    return IDomainResult.Success(state.Cache);
  }

  #region GetTermsOfService
  public (string?, IDomainResult) GetTermsOfServiceUri(Guid sessionId) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(GetTermsOfServiceUri)}...");
      if (state.Directory?.Meta?.TermsOfService == null) {
        return IDomainResult.Failed<string?>();
      }
      return IDomainResult.Success(state.Directory.Meta.TermsOfService);
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<string?>(message);
    }
  }
  #endregion

  #region NewOrder
  public async Task<(Dictionary<string, string>?, IDomainResult)> NewOrder(Guid sessionId, string[] hostnames, string challengeType) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(NewOrder)}...");
      state.Challenges.Clear();
      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames?.Where(h => h != null).Select(hostname => new OrderIdentifier {
          Type = DnsType,
          Value = hostname ?? string.Empty
        }).ToArray() ?? Array.Empty<OrderIdentifier>()
      };
      if (state.Directory == null || state.Directory.NewOrder == null)
        return (null, IDomainResult.Failed());
      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewOrder);
      await HandleNonceAsync(sessionId, state.Directory.NewOrder, state);
      var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
        Url = state.Directory.NewOrder,
        Nonce = state.Nonce
      });
      PrepareRequestContent(request, json, HttpMethod.Post);
      var order = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
      if (StatusEquals(order.Result?.Status, OrderStatus.Ready))
        return (new Dictionary<string, string>(), IDomainResult.Success());
      if (!StatusEquals(order.Result?.Status, OrderStatus.Pending)) {
        _logger.LogError($"Created new order and expected status '{OrderStatus.Pending.GetDisplayName()}', but got: {order.Result?.Status} \r\n {order.Result}");
        return (null, IDomainResult.Failed());
      }
      state.CurrentOrder = order.Result;
      var results = new Dictionary<string, string>();
      foreach (var item in state.CurrentOrder?.Authorizations ?? Array.Empty<Uri>()) {
        if (item == null) continue;
        request = new HttpRequestMessage(HttpMethod.Post, item);
        await HandleNonceAsync(sessionId, item, state);
        json = EncodeMessage(true, null, state, new JwsHeader {
          Url = item,
          Nonce = state.Nonce
        });
        PrepareRequestContent(request, json, HttpMethod.Post);
        var challengeResponse = await SendAcmeRequest<AuthorizationChallengeResponse>(request, state, HttpMethod.Post);
        if (StatusEquals(challengeResponse.Result?.Status, OrderStatus.Valid))
          continue;
        if (!StatusEquals(challengeResponse.Result?.Status, OrderStatus.Pending)) {
          _logger.LogError($"Expected authorization status '{OrderStatus.Pending.GetDisplayName()}', but got: {state.CurrentOrder?.Status} \r\n {challengeResponse.ResponseText}");
          return (null, IDomainResult.Failed());
        }
        var challenge = challengeResponse.Result?.Challenges?.FirstOrDefault(x => x?.Type == challengeType);
        if (challenge == null || challenge.Token == null) {
          _logger.LogError("Challenge or token is null");
          return (null, IDomainResult.Failed());
        }
        state.Challenges.Add(challenge);
        if (state.Cache != null) state.Cache.ChallengeType = challengeType;
        var keyToken = state.JwsService != null ? state.JwsService.GetKeyAuthorization(challenge.Token) : string.Empty;
        switch (challengeType) {
          case "dns-01":
            using (var sha256 = SHA256.Create()) {
              var dnsToken = state.JwsService != null ? state.JwsService.Base64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyToken ?? string.Empty))) : string.Empty;
              results[challengeResponse.Result?.Identifier?.Value ?? string.Empty] = dnsToken;
            }
            break;
          case "http-01":
            results[challengeResponse.Result?.Identifier?.Value ?? string.Empty] = keyToken ?? string.Empty;
            break;
          default:
            throw new NotImplementedException();
        }
      }
      return (results, IDomainResult.Success());
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return (null, IDomainResult.CriticalDependencyError(message));
    }
  }
  #endregion

  #region CompleteChallenges
  public async Task<IDomainResult> CompleteChallenges(Guid sessionId) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(CompleteChallenges)}...");
      if (state.CurrentOrder?.Identifiers == null) {
        return IDomainResult.Failed("Current order identifiers are null");
      }
      for (var index = 0; index < state.Challenges.Count; index++) {
        var challenge = state.Challenges[index];
        if (challenge?.Url == null) {
          _logger.LogError("Challenge URL is null");
          return IDomainResult.Failed();
        }
        var request = new HttpRequestMessage(HttpMethod.Post, challenge.Url);
        await HandleNonceAsync(sessionId, challenge.Url, state);
        var json = EncodeMessage(false, "{}", state, new JwsHeader {
          Url = challenge.Url,
          Nonce = state.Nonce
        });
        PrepareRequestContent(request, json, HttpMethod.Post);
        var authChallenge = await SendAcmeRequest<AuthorizationChallengeResponse>(request, state, HttpMethod.Post);
        var result = await PollChallengeStatus(sessionId, challenge, state);
        if (!result.IsSuccess)
          return result;
      }
      return IDomainResult.Success();
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }
  #endregion

  #region GetOrder
  public async Task<IDomainResult> GetOrder(Guid sessionId, string[] hostnames) {
    try {
      _logger.LogInformation($"Executing {nameof(GetOrder)}");
      var state = GetOrCreateState(sessionId);
      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames?.Where(h => h != null).Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname!
        }).ToArray() ?? Array.Empty<OrderIdentifier>()
      };
      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory!.NewOrder);
      await HandleNonceAsync(sessionId, state.Directory.NewOrder, state);
      var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
        Url = state.Directory.NewOrder,
        Nonce = state.Nonce
      });
      PrepareRequestContent(request, json, HttpMethod.Post);
      var order = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
      state.CurrentOrder = order.Result;
      return IDomainResult.Success();
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }
  #endregion

  #region GetCertificates
  public async Task<IDomainResult> GetCertificate(Guid sessionId, string subject) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(GetCertificate)}...");
      if (state.CurrentOrder?.Identifiers == null) {
        return IDomainResult.Failed();
      }
      var key = new RSACryptoServiceProvider(4096);
      var csr = new CertificateRequest("CN=" + subject,
          key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
      var san = new SubjectAlternativeNameBuilder();
      foreach (var host in state.CurrentOrder.Identifiers) {
        if (host?.Value != null)
          san.AddDnsName(host.Value);
      }
      csr.CertificateExtensions.Add(san.Build());
      var letsEncryptOrder = new FinalizeRequest {
        Csr = state.JwsService!.Base64UrlEncoded(csr.CreateSigningRequest())
      };
      Uri? certificateUrl = default;
      var start = DateTime.UtcNow;
      while (certificateUrl == null) {
        var hostnames = state.CurrentOrder.Identifiers?.Select(x => x?.Value).Where(x => x != null).Cast<string>().ToArray() ?? Array.Empty<string>();
        await GetOrder(sessionId, hostnames);
        var status = state.CurrentOrder?.Status;
        if (StatusEquals(status, OrderStatus.Ready)) {
          var request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Finalize!);
          await HandleNonceAsync(sessionId, state.CurrentOrder.Finalize!, state);
          var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
            Url = state.CurrentOrder.Finalize,
            Nonce = state.Nonce
          });
          PrepareRequestContent(request, json, HttpMethod.Post);
          var order = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
          if (StatusEquals(order.Result?.Status, OrderStatus.Processing)) {
            request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Location!);
            await HandleNonceAsync(sessionId, state.CurrentOrder.Location!, state);
            json = EncodeMessage(true, null, state, new JwsHeader {
              Url = state.CurrentOrder.Location,
              Nonce = state.Nonce
            });
            PrepareRequestContent(request, json, HttpMethod.Post);
            order = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
          }
          if (StatusEquals(order.Result?.Status, OrderStatus.Valid)) {
            certificateUrl = order.Result.Certificate;
          }
        } else if (StatusEquals(status, OrderStatus.Valid)) {
          certificateUrl = state.CurrentOrder.Certificate;
          break;
        }
        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();
        await Task.Delay(1000);
      }
      var finalRequest = new HttpRequestMessage(HttpMethod.Post, certificateUrl!);
      await HandleNonceAsync(sessionId, certificateUrl!, state);
      var finalJson = EncodeMessage(true, null, state, new JwsHeader {
        Url = certificateUrl,
        Nonce = state.Nonce
      });
      PrepareRequestContent(finalRequest, finalJson, HttpMethod.Post);
      var pem = await SendAcmeRequest<string>(finalRequest, state, HttpMethod.Post);
      if (state.Cache == null) {
        _logger.LogError($"{nameof(state.Cache)} is null");
        return IDomainResult.Failed();
      }
      state.Cache.CachedCerts ??= new Dictionary<string, CertificateCache>();
      state.Cache.CachedCerts[subject] = new CertificateCache {
        Cert = pem.Result ?? string.Empty,
        Private = key.ExportCspBlob(true),
        PrivatePem = key.ExportRSAPrivateKeyPem()
      };
      var certPem = pem.Result ?? string.Empty;
      if (!string.IsNullOrEmpty(certPem)) {
        var cert = new X509Certificate2(Encoding.UTF8.GetBytes(certPem));
      }
      return IDomainResult.Success();
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }
  #endregion

  public Task<IDomainResult> KeyChange(Guid sessionId) {
    throw new NotImplementedException();
  }

  public async Task<IDomainResult> RevokeCertificate(Guid sessionId, string subject, RevokeReason reason) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(RevokeCertificate)}...");
      if (state.Cache?.CachedCerts == null || !state.Cache.CachedCerts.TryGetValue(subject, out var certificateCache) || certificateCache == null) {
        _logger.LogError("Certificate not found in cache");
        return IDomainResult.Failed("Certificate not found");
      }
      var certPem = certificateCache.Cert ?? string.Empty;
      if (string.IsNullOrEmpty(certPem)) {
        _logger.LogError("Certificate PEM is null or empty");
        return IDomainResult.Failed("Certificate PEM is null or empty");
      }
      var certificate = new X509Certificate2(Encoding.UTF8.GetBytes(certPem));
      var derEncodedCert = certificate.Export(X509ContentType.Cert);
      var base64UrlEncodedCert = state.JwsService!.Base64UrlEncoded(derEncodedCert);
      var revokeRequest = new RevokeRequest {
        Certificate = base64UrlEncodedCert,
        Reason = (int)reason
      };
      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory!.RevokeCert);
      await HandleNonceAsync(sessionId, state.Directory.RevokeCert, state);
      var jwsHeader = new JwsHeader {
        Url = state.Directory.RevokeCert,
        Nonce = state.Nonce
      };
      var json = state.JwsService.Encode(revokeRequest, jwsHeader).ToJson();
      request.Content = new StringContent(json);
      request.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(ContentType.JoseJson));
      var response = await _httpClient.SendAsync(request);
      UpdateStateNonceIfNeeded(response, state, HttpMethod.Post);
      var responseText = await response.Content.ReadAsStringAsync();
      if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.ProblemJson)) {
        var erroObj = responseText.ToObject<Problem>();
      }
      if (!response.IsSuccessStatusCode)
        IDomainResult.CriticalDependencyError(responseText);
      state.Cache.CachedCerts.Remove(subject);
      _logger.LogInformation("Certificate revoked successfully");
      return IDomainResult.Success();
    } catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError($"{message}: {ex.Message}");
    }
  }

  #region SendAsync
  private async Task HandleNonceAsync(Guid sessionId, Uri uri, State state) {
    if (uri == null) throw new ArgumentNullException(nameof(uri));
    if (uri.OriginalString != "directory") {
      var (nonce, newNonceResult) = await NewNonce(sessionId);
      if (!newNonceResult.IsSuccess || nonce == null) {
        throw new InvalidOperationException("Failed to retrieve nonce.");
      }
      state.Nonce = nonce;
    }
    else {
      state.Nonce = default;
    }
  }

  private async Task<(string?, IDomainResult)> NewNonce(Guid sessionId) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(NewNonce)}...");
      if (state.Directory?.NewNonce == null)
        return (null, IDomainResult.Failed());
      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, state.Directory.NewNonce));
      var nonce = result.Headers.GetValues("Replay-Nonce").FirstOrDefault();
      return (nonce, IDomainResult.Success());
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return (null, IDomainResult.CriticalDependencyError(message));
    }
  }

  private string EncodeMessage(bool isPostAsGet, object? requestModel, State state, JwsHeader jwsHeader) {
    return isPostAsGet
        ? state.JwsService!.Encode(jwsHeader).ToJson()
        : state.JwsService!.Encode(requestModel, jwsHeader).ToJson();
  }

  private static string GetContentType(ContentType type) => type.GetDisplayName();

  private void PrepareRequestContent(HttpRequestMessage request, string json, HttpMethod method) {
    request.Content = new StringContent(json ?? string.Empty);
    var contentType = method == HttpMethod.Post ? GetContentType(ContentType.JoseJson) : GetContentType(ContentType.Json);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
  }

  private void HandleProblemResponseAsync(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.ProblemJson)) {
      throw new LetsEncrytException(responseText.ToObject<Problem>(), response);
    }
  }

  private SendResult<TResult> ProcessResponseContent<TResult>(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.PemCertificateChain) && typeof(TResult) == typeof(string)) {
      return new SendResult<TResult> {
        Result = (TResult)(object)responseText
      };
    }
    var responseContent = responseText.ToObject<TResult>();
    if (responseContent is IHasLocation ihl && response.Headers.Location != null) {
      ihl.Location = response.Headers.Location;
    }
    return new SendResult<TResult> {
      Result = responseContent,
      ResponseText = responseText
    };
  }
  #endregion

  private void UpdateStateNonceIfNeeded(HttpResponseMessage response, State state, HttpMethod method) {
    if (method == HttpMethod.Post && response.Headers.Contains(ReplayNonceHeader)) {
      state.Nonce = response.Headers.GetValues(ReplayNonceHeader).FirstOrDefault();
    }
  }

  // Helper for status comparison
  private static bool StatusEquals(string? status, OrderStatus expected) => status == expected.GetDisplayName();
}
