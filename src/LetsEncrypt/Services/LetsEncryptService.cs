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
  private readonly ILogger<LetsEncryptService> _logger;
  private readonly HttpClient _httpClient;
  private readonly IMemoryCache _memoryCache;

  public LetsEncryptService(
      ILogger<LetsEncryptService> logger,
      HttpClient httpClient,
      IMemoryCache cache) {
    _logger = logger;
    _httpClient = httpClient;
    _memoryCache = cache;
  }

  private State GetOrCreateState(Guid sessionId) {
    if (!_memoryCache.TryGetValue(sessionId, out State state)) {
      state = new State();
      _memoryCache.Set(sessionId, state, TimeSpan.FromHours(1));
    }
    return state;
  }

  #region ConfigureClient
  public async Task<IDomainResult> ConfigureClient(Guid sessionId, bool isStaging) {
    try {
      var state = GetOrCreateState(sessionId);

      state.IsStaging = isStaging;
      // TODO: need to propagate from Configuration
      _httpClient.BaseAddress ??= new Uri(isStaging
        ? "https://acme-staging-v02.api.letsencrypt.org/directory"
        : "https://acme-v02.api.letsencrypt.org/directory");

      if (state.Directory == null) {
          var request = new HttpRequestMessage(HttpMethod.Get, new Uri("directory", UriKind.Relative));
          await HandleNonceAsync(sessionId, new Uri("directory", UriKind.Relative), state);

          var response = await _httpClient.SendAsync(request);
          UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Get);

          var responseText = await response.Content.ReadAsStringAsync();            
          HandleProblemResponseAsync(response, responseText);

          var directory = ProcessResponseContent<AcmeDirectory>(response, responseText);

          state.Directory = directory.Result;
      }

      return IDomainResult.Success();
    }
    catch (Exception ex) {
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
        state.JwsService.SetKeyId(cache.Location.ToString());
      }
      else {
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

        var response = await _httpClient.SendAsync(request);
        UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

        var responseText = await response.Content.ReadAsStringAsync();
        HandleProblemResponseAsync(response, responseText);

        var result = ProcessResponseContent<Account>(response, responseText);

        state.JwsService.SetKeyId(result.Result.Location.ToString());

        if (result.Result.Status != "valid") {
          _logger.LogError($"Account status is not valid, was: {result.Result.Status} \r\n {result.ResponseText}");
          return IDomainResult.Failed();
        }

        state.Cache = new RegistrationCache {
          AccountId = accountId,
          Description = description,
          Contacts = contacts,
          IsStaging = state.IsStaging,
          Location = result.Result.Location,
          AccountKey = accountKey.ExportCspBlob(true),
          Id = result.Result.Id,
          Key = result.Result.Key
        };
      }

      return IDomainResult.Success();
    }
    catch (Exception ex) {
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

      if (state.Directory == null) {
        return IDomainResult.Failed<string?>();
      }

      return IDomainResult.Success(state.Directory.Meta.TermsOfService);
    }
    catch (Exception ex) {
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
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewOrder);
      await HandleNonceAsync(sessionId, state.Directory.NewOrder, state);

      var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
        Url = state.Directory.NewOrder,
        Nonce = state.Nonce
      });
      PrepareRequestContent(request, json, HttpMethod.Post);
 
      var response = await _httpClient.SendAsync(request);
      UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

      var responseText = await response.Content.ReadAsStringAsync();
      HandleProblemResponseAsync(response, responseText);

      var order = ProcessResponseContent<Order>(response, responseText);

      if (order.Result.Status == "ready")
        return IDomainResult.Success(new Dictionary<string, string>());

      if (order.Result.Status != "pending") {
        _logger.LogError($"Created new order and expected status 'pending', but got: {order.Result.Status} \r\n {order.Result}");
        return IDomainResult.Failed<Dictionary<string, string>?>();
      }

      state.CurrentOrder = order.Result;
      
      var results = new Dictionary<string, string>();
      foreach (var item in state.CurrentOrder.Authorizations) {

        request = new HttpRequestMessage(HttpMethod.Post, item);
        await HandleNonceAsync(sessionId, item, state);

        json = EncodeMessage(true, null, state, new JwsHeader {
          Url = item,
          Nonce = state.Nonce
        });
        PrepareRequestContent(request, json, HttpMethod.Post);

          

        response = await _httpClient.SendAsync(request);
        UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

        responseText = await response.Content.ReadAsStringAsync();
        HandleProblemResponseAsync(response, responseText);

        var challengeResponse = ProcessResponseContent<AuthorizationChallengeResponse>(response, responseText);


        if (challengeResponse.Result.Status == "valid")
          continue;

        if (challengeResponse.Result.Status != "pending") {
          _logger.LogError($"Expected authorization status 'pending', but got: {state.CurrentOrder.Status} \r\n {challengeResponse.ResponseText}");
          return IDomainResult.Failed<Dictionary<string, string>?>();
        }

        var challenge = challengeResponse.Result.Challenges.First(x => x.Type == challengeType);
        state.Challenges.Add(challenge);
        state.Cache.ChallengeType = challengeType;

        var keyToken = state.JwsService.GetKeyAuthorization(challenge.Token);

        switch (challengeType) {
          case "dns-01":
            using (var sha256 = SHA256.Create()) {
              var dnsToken = state.JwsService.Base64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyToken)));
              results[challengeResponse.Result.Identifier.Value] = dnsToken;
            }
            break;

          case "http-01":
            results[challengeResponse.Result.Identifier.Value] = keyToken;
            break;

          default:
            throw new NotImplementedException();
        }
      }

      return IDomainResult.Success(results);
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<Dictionary<string, string>?>(message);
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
        var start = DateTime.UtcNow;

        while (true) {
          var authorizeChallenge = new AuthorizeChallenge();

          switch (challenge.Type) {
            case "dns-01":
              authorizeChallenge.KeyAuthorization = state.JwsService.GetKeyAuthorization(challenge.Token);
              break;

            case "http-01":
              break;
          }

          var request = new HttpRequestMessage(HttpMethod.Post, challenge.Url);
          await HandleNonceAsync(sessionId, challenge.Url, state);

          var json = EncodeMessage(false, "{}", state, new JwsHeader {
            Url = challenge.Url,
            Nonce = state.Nonce
          });
          PrepareRequestContent(request, json, HttpMethod.Post);

          var response = await _httpClient.SendAsync(request);
          UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

          var responseText = await response.Content.ReadAsStringAsync();
          HandleProblemResponseAsync(response, responseText);

          var authChallenge = ProcessResponseContent<AuthorizationChallengeResponse>(response, responseText);
          //return IDomainResult.Success(result);


          if (authChallenge.Result.Status == "valid")
            break;

          if (authChallenge.Result.Status != "pending") {
            _logger.LogError($"Challenge failed with status {authChallenge.Result.Status} \r\n {authChallenge.ResponseText}");
            return IDomainResult.Failed();
          }

          await Task.Delay(1000);

          if ((DateTime.UtcNow - start).Seconds > 120)
            return IDomainResult.Failed("Timeout");
        }
      }

      return IDomainResult.Success();
    }
    catch (Exception ex) {
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
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewOrder);
      await HandleNonceAsync(sessionId, state.Directory.NewOrder, state);
      
      var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
        Url = state.Directory.NewOrder,
        Nonce = state.Nonce
      });
      PrepareRequestContent(request, json, HttpMethod.Post);

      var response = await _httpClient.SendAsync(request);
      UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

      var responseText = await response.Content.ReadAsStringAsync();
      HandleProblemResponseAsync(response, responseText);

      var order = ProcessResponseContent<Order>(response, responseText);
      state.CurrentOrder = order.Result;
      
      return IDomainResult.Success();
    }
    catch (Exception ex) {
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

      if (state.CurrentOrder == null) {
        return IDomainResult.Failed();
      }

      var key = new RSACryptoServiceProvider(4096);
      var csr = new CertificateRequest("CN=" + subject,
          key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

      var san = new SubjectAlternativeNameBuilder();
      foreach (var host in state.CurrentOrder.Identifiers)
        san.AddDnsName(host.Value);

      csr.CertificateExtensions.Add(san.Build());

      var letsEncryptOrder = new FinalizeRequest {
        Csr = state.JwsService.Base64UrlEncoded(csr.CreateSigningRequest())
      };

      Uri? certificateUrl = default;

      var start = DateTime.UtcNow;

      while (certificateUrl == null) {
        // https://community.letsencrypt.org/t/breaking-changes-in-asynchronous-order-finalization-api/195882
        await GetOrder(sessionId, state.CurrentOrder.Identifiers.Select(x => x.Value).ToArray());

        if (state.CurrentOrder.Status == "ready") {
          
          
          var request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Finalize);
          await HandleNonceAsync(sessionId, state.CurrentOrder.Finalize, state);

          var json = EncodeMessage(false, letsEncryptOrder, state, new JwsHeader {
            Url = state.CurrentOrder.Finalize,
            Nonce = state.Nonce
          });
          PrepareRequestContent(request, json, HttpMethod.Post);

          var response = await _httpClient.SendAsync(request);
          UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

          var responseText = await response.Content.ReadAsStringAsync();
          HandleProblemResponseAsync(response, responseText);

          var order = ProcessResponseContent<Order>(response, responseText);



          if (order.Result.Status == "processing") {

            request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Location);
            await HandleNonceAsync(sessionId, state.CurrentOrder.Location, state);

            json = EncodeMessage(true, null, state, new JwsHeader {
              Url = state.CurrentOrder.Location,
              Nonce = state.Nonce
            });
            PrepareRequestContent(request, json, HttpMethod.Post);

            response = await _httpClient.SendAsync(request);
            UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

            responseText = await response.Content.ReadAsStringAsync();
            HandleProblemResponseAsync(response, responseText);

            order = ProcessResponseContent<Order>(response, responseText);
          }

          if (order.Result.Status == "valid") {
            certificateUrl = order.Result.Certificate;
          }
        }

        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
      }     

      

      var finalRequest = new HttpRequestMessage(HttpMethod.Post, certificateUrl);
      await HandleNonceAsync(sessionId, certificateUrl, state);

      var finalJson = EncodeMessage(true, null, state, new JwsHeader {
        Url = certificateUrl,
        Nonce = state.Nonce
      });
      PrepareRequestContent(finalRequest, finalJson, HttpMethod.Post);

      var finalResponse = await _httpClient.SendAsync(finalRequest);
      UpdateStateNonceIfNeededAsync(finalResponse, state, HttpMethod.Post);

      var finalResponseText = await finalResponse.Content.ReadAsStringAsync();
      HandleProblemResponseAsync(finalResponse, finalResponseText);

      var pem = ProcessResponseContent<string>(finalResponse, finalResponseText);
        

      if (state.Cache == null) {
        _logger.LogError($"{nameof(state.Cache)} is null");
        return IDomainResult.Failed();
      }

      state.Cache.CachedCerts ??= new Dictionary<string, CertificateCache>();
      state.Cache.CachedCerts[subject] = new CertificateCache {
        Cert = pem.Result,
        Private = key.ExportCspBlob(true),
        PrivatePem = key.ExportRSAPrivateKeyPem()
      };

      var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pem.Result));
      


      return IDomainResult.Success();
    }
    catch (Exception ex) {
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

      if (state.Cache == null || state.Cache.CachedCerts == null || !state.Cache.CachedCerts.TryGetValue(subject, out var certificateCache)) {
        _logger.LogError("Certificate not found in cache");
        return IDomainResult.Failed("Certificate not found");
      }

      // Load the certificate from PEM format and convert it to DER format
      var certificate = new X509Certificate2(Encoding.UTF8.GetBytes(certificateCache.Cert));
      var derEncodedCert = certificate.Export(X509ContentType.Cert);
      var base64UrlEncodedCert = state.JwsService.Base64UrlEncoded(derEncodedCert);


      var revokeRequest = new RevokeRequest {
        Certificate = base64UrlEncodedCert,
        Reason = (int)reason
      };

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.RevokeCert);
      await HandleNonceAsync(sessionId, state.Directory.RevokeCert, state);

      var jwsHeader = new JwsHeader {
        Url = state.Directory.RevokeCert,
        Nonce = state.Nonce
      };

      var json = state.JwsService.Encode(revokeRequest, jwsHeader).ToJson();
      
      request.Content = new StringContent(json);
      request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/jose+json");

      var response = await _httpClient.SendAsync(request);
      UpdateStateNonceIfNeededAsync(response, state, HttpMethod.Post);

      var responseText = await response.Content.ReadAsStringAsync();
      if (response.Content.Headers.ContentType?.MediaType == "application/problem+json") {
        var erroObj = responseText.ToObject<Problem>();
      }

      if (!response.IsSuccessStatusCode)
        IDomainResult.CriticalDependencyError(responseText);

      
      // Remove the certificate from the cache after successful revocation
      state.Cache.CachedCerts.Remove(subject);

      _logger.LogInformation("Certificate revoked successfully");
      
      return IDomainResult.Success();
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError($"{message}: {ex.Message}");
    }
  }

  #region SendAsync
  private async Task HandleNonceAsync(Guid sessionId, Uri uri, State state) {
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

      if (state.Directory == null)
        IDomainResult.Failed();

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, state.Directory.NewNonce));
      return IDomainResult.Success(result.Headers.GetValues("Replay-Nonce").First());
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<string?>(message);
    }
  }

  private string EncodeMessage(bool isPostAsGet, object? requestModel, State state, JwsHeader jwsHeader) {
    return isPostAsGet
        ? state.JwsService.Encode(jwsHeader).ToJson()
        : state.JwsService.Encode(requestModel, jwsHeader).ToJson();
  }

  private void PrepareRequestContent(HttpRequestMessage request, string json, HttpMethod method) {
    request.Content = new StringContent(json);
    var contentType = method == HttpMethod.Post ? "application/jose+json" : "application/json";
    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
  }

  private void UpdateStateNonceIfNeededAsync(HttpResponseMessage response, State state, HttpMethod method) {
    if (method == HttpMethod.Post && response.Headers.Contains("Replay-Nonce")) {
      state.Nonce = response.Headers.GetValues("Replay-Nonce").First();
    }
  }

  private void HandleProblemResponseAsync(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == "application/problem+json") {
      throw new LetsEncrytException(responseText.ToObject<Problem>(), response);
    }
  }

  private SendResult<TResult> ProcessResponseContent<TResult>(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == "application/pem-certificate-chain" && typeof(TResult) == typeof(string)) {
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
}
