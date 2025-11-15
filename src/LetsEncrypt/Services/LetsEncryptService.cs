/**
 * https://datatracker.ietf.org/doc/html/rfc8555
 * https://datatracker.ietf.org/doc/html/draft-ietf-acme-acme-12
 */

using MaksIT.Core.Extensions;
using MaksIT.Core.Security;
using MaksIT.Core.Security.JWK;
using MaksIT.Core.Security.JWS;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Entities.Jws;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;
using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.LetsEncrypt.Models.Interfaces;
using MaksIT.LetsEncrypt.Models.Requests;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace MaksIT.LetsEncrypt.Services;

public interface ILetsEncryptService {
  Result<RegistrationCache?> GetRegistrationCache(Guid sessionId);
  Task<Result> ConfigureClient(Guid sessionId, bool isStaging);
  Task<Result> Init(Guid sessionId,Guid accountId, string description, string[] contacts, RegistrationCache? registrationCache);
  Result<string?> GetTermsOfServiceUri(Guid sessionId);
  Task<Result<Dictionary<string, string>?>> NewOrder(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> CompleteChallenges(Guid sessionId);
  Task<Result> GetOrder(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificate(Guid sessionId, string subject);
  Task<Result> RevokeCertificate(Guid sessionId, string subject, RevokeReason reason);
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

  public Result<RegistrationCache?> GetRegistrationCache(Guid sessionId) {
    var state = GetOrCreateState(sessionId);

    if (state?.Cache == null)
      return Result<RegistrationCache?>.InternalServerError(null);

    return Result<RegistrationCache?>.Ok(state.Cache);
  }

  #region ConfigureClient
  public async Task<Result> ConfigureClient(Guid sessionId, bool isStaging) {
    try {
      var state = GetOrCreateState(sessionId);

      state.IsStaging = isStaging;

      _httpClient.BaseAddress ??= new Uri(isStaging ? _appSettings.Staging : _appSettings.Production);

      if (state.Directory == null) {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(DirectoryEndpoint, UriKind.Relative));

        var requestResult = await SendAcmeRequest<AcmeDirectory>(request, state, HttpMethod.Get);
        if (!requestResult.IsSuccess || requestResult.Value == null)
          return requestResult;

      var directory = requestResult.Value;

        state.Directory = directory.Result ?? throw new InvalidOperationException("Directory response is null");
      }

      return Result.Ok("Client configured successfully.");
    }
    catch (LetsEncrytException ex) {
      return HandleUnhandledException(ex, "Let's Encrypt client encountered a problem");
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region Init
  public async Task<Result> Init(Guid sessionId, Guid accountId, string description, string[] contacts, RegistrationCache? cache) {
    if (sessionId == Guid.Empty) {
      const string message = "Invalid sessionId";
      _logger.LogError(message);
      return Result.InternalServerError(message);
    }

    if (contacts == null || contacts.Length == 0) {
      const string message = "Contacts are null or empty";
      _logger.LogError(message);
      return Result.InternalServerError(message);
    }

    var state = GetOrCreateState(sessionId);

    if (state.Directory == null) {
      const string message = "State directory is null";
      _logger.LogError(message);
      return Result.InternalServerError(message);
    }

    _logger.LogInformation($"Executing {nameof(Init)}...");

    try {
      var accountKey = new RSACryptoServiceProvider(4096);

      if (cache != null && cache.AccountKey != null) {
        state.Cache = cache;
        
        accountKey.ImportCspBlob(cache.AccountKey);
        
        if (!JwkGenerator.TryGenerateFromRSA(accountKey, out var jwk, out var errorMessage)) {
          return Result.InternalServerError(errorMessage);
        }

        state.Rsa = accountKey;
        state.Jwk = jwk;

        state.Jwk.KeyId = cache.Location?.ToString() ?? string.Empty;
      }
      else {
        if (!JwkGenerator.TryGenerateFromRSA(accountKey, out var jwk, out var errorMessage)) {
          return Result.InternalServerError(errorMessage);
        }

        state.Rsa = accountKey;
        state.Jwk = jwk;

        var letsEncryptOrder = new Account {
          TermsOfServiceAgreed = true,
          Contacts = [.. contacts.Select(contact => $"mailto:{contact}")]
        };

        var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewAccount);

        var nonceResult = await GetNonceAsync(sessionId, state.Directory.NewAccount);
        if (!nonceResult.IsSuccess || nonceResult.Value == null)
          return nonceResult;

        var nonce = nonceResult.Value;

        var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
          Url = state.Directory.NewAccount.ToString(),
          Nonce = nonce
        });

        if (!jsonResult.IsSuccess || jsonResult.Value == null)
          return jsonResult;

        var json = jsonResult.Value;

        PrepareRequestContent(request, json, HttpMethod.Post);

        var requestResult = await SendAcmeRequest<Account>(request, state, HttpMethod.Post);
        if (!requestResult.IsSuccess || requestResult.Value == null)
          return requestResult;

        var result = requestResult.Value;

        state.Jwk.KeyId = result.Result?.Location?.ToString() ?? string.Empty;

        if (result.Result?.Status != "valid") {
          errorMessage = $"Account status is not valid, was: {result.Result?.Status} \r\n {result.ResponseText}";
          _logger.LogError(errorMessage);
          return Result.InternalServerError(errorMessage);
        }

        state.Cache = new RegistrationCache {
          AccountId = accountId,
          Description = description,
          Contacts = contacts,
          IsStaging = state.IsStaging,
          ChallengeType = ChalengeType.http.GetDisplayName(),
          Location = result.Result.Location,
          AccountKey = accountKey.ExportCspBlob(true),
          Id = result.Result.Id ?? string.Empty,
          Key = result.Result.Key
        };
      }

      return Result.Ok("Initialization successful.");
    }
    catch (LetsEncrytException ex) {
      return HandleUnhandledException(ex, "Let's Encrypt client encountered a problem");
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region GetTermsOfService
  public Result<string?> GetTermsOfServiceUri(Guid sessionId) {
    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(GetTermsOfServiceUri)}...");

      if (state.Directory?.Meta?.TermsOfService == null) {
        return Result<string?>.Ok(null);
      }

      return Result<string?>.Ok(state.Directory.Meta.TermsOfService);
    }
    catch (Exception ex) {
      return HandleUnhandledException<string?>(ex);
    }
  }
  #endregion

  #region NewOrder
  public async Task<Result<Dictionary<string, string>?>> NewOrder(Guid sessionId, string[] hostnames, string challengeType) {
    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(NewOrder)}...");

      state.Challenges.Clear();

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames?.Where(h => h != null).Select(hostname => new OrderIdentifier {
          Type = DnsType,
          Value = hostname ?? string.Empty
        }).ToArray() ?? []
      };

      if (state.Directory == null || state.Directory.NewOrder == null)
        return Result<Dictionary<string, string>?>.InternalServerError(null);

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory.NewOrder);

      var nonceResult = await GetNonceAsync(sessionId, state.Directory.NewOrder);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

      var nonce = nonceResult.Value;

      var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
        Url = state.Directory.NewOrder.ToString(),
        Nonce = nonce
      });

      if (!jsonResult.IsSuccess || jsonResult.Value == null)
        return jsonResult.ToResultOfType <Dictionary<string, string>?>(_ => null);

      var json = jsonResult.Value;

      PrepareRequestContent(request, json, HttpMethod.Post);

      var requestResult = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
      if (!requestResult.IsSuccess || requestResult.Value == null)
        return requestResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

      var order = requestResult.Value;

      if (StatusEquals(order.Result?.Status, OrderStatus.Ready))
        return Result<Dictionary<string, string>?>.Ok(new Dictionary<string, string>());

      if (!StatusEquals(order.Result?.Status, OrderStatus.Pending)) {
        _logger.LogError($"Created new order and expected status '{OrderStatus.Pending.GetDisplayName()}', but got: {order.Result?.Status} \r\n {order.Result}");
        return Result<Dictionary<string, string>?>.InternalServerError(null);
      }

      state.CurrentOrder = order.Result;

      var results = new Dictionary<string, string>();

      foreach (var item in state.CurrentOrder?.Authorizations ?? Array.Empty<Uri>()) {
        if (item == null)
          continue;

        request = new HttpRequestMessage(HttpMethod.Post, item);

        nonceResult = await GetNonceAsync(sessionId, item);
        if (!nonceResult.IsSuccess || nonceResult.Value == null)
          return nonceResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

        nonce = nonceResult.Value;

        jsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
          Url = item.ToString(),
          Nonce = nonce
        });

        if (!jsonResult.IsSuccess || jsonResult.Value == null)
          return jsonResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

        json = jsonResult.Value;

        PrepareRequestContent(request, json, HttpMethod.Post);

        var challengeResult = await SendAcmeRequest<AuthorizationChallengeResponse>(request, state, HttpMethod.Post);
        if (!challengeResult.IsSuccess || challengeResult.Value == null)
          return challengeResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

        var challengeResponse = challengeResult.Value;

        if (StatusEquals(challengeResponse.Result?.Status, OrderStatus.Valid))
          continue;

        if (!StatusEquals(challengeResponse.Result?.Status, OrderStatus.Pending)) {
          _logger.LogError($"Expected authorization status '{OrderStatus.Pending.GetDisplayName()}', but got: {state.CurrentOrder?.Status} \r\n {challengeResponse.ResponseText}");
          return Result<Dictionary<string, string>?>.InternalServerError(null);
        }

        var challenge = challengeResponse.Result?.Challenges?
          .FirstOrDefault(x => x?.Type == challengeType);

        if (challenge == null || challenge.Token == null) {
          _logger.LogError("Challenge or token is null");
          return Result<Dictionary<string, string>?>.InternalServerError(null);
        }

        state.Challenges.Add(challenge);
        
        if (state.Cache != null)
          state.Cache.ChallengeType = challengeType;

        if (!JwkThumbprintUtility.TryGetKeyAuthorization(state.Jwk, challenge.Token, out var keyToken, out var errorMessage))
          return Result<Dictionary<string, string>?>.InternalServerError(null, errorMessage);

        switch (challengeType) {
          case "dns-01":
            using (var sha256 = SHA256.Create()) {
              var dnsToken = Base64UrlUtility.Encode(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyToken ?? string.Empty)));

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

      return Result<Dictionary<string, string>?>.Ok(results);
    }
    catch (Exception ex) {
      return HandleUnhandledException<Dictionary<string, string>?>(ex);
    }
  }
  #endregion

  #region CompleteChallenges
  public async Task<Result> CompleteChallenges(Guid sessionId) {
    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(CompleteChallenges)}...");

      if (state.CurrentOrder?.Identifiers == null) {
        return Result.InternalServerError("Current order identifiers are null");
      }

      for (var index = 0; index < state.Challenges.Count; index++) {
        var challenge = state.Challenges[index];

        if (challenge?.Url == null) {
          _logger.LogError("Challenge URL is null");
          return Result.InternalServerError("Challenge URL is null");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, challenge.Url);

        var nonceResult = await GetNonceAsync(sessionId, challenge.Url);
        if (!nonceResult.IsSuccess || nonceResult.Value == null)
          return nonceResult;

        var nonce = nonceResult.Value;

        var jsonResult = EncodeMessage(sessionId, false, "{}", new ACMEJwsHeader {
          Url = challenge.Url.ToString(),
          Nonce = nonce
        });

        if (!jsonResult.IsSuccess || jsonResult.Value == null)
          return jsonResult;

        var json = jsonResult.Value;

        PrepareRequestContent(request, json, HttpMethod.Post);

        var authChallenge = await SendAcmeRequest<AuthorizationChallengeResponse>(request, state, HttpMethod.Post);

        var result = await PollChallengeStatus(sessionId, challenge);

        if (!result.IsSuccess)
          return result;
      }
      return Result.Ok();
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region GetOrder
  public async Task<Result> GetOrder(Guid sessionId, string[] hostnames) {
    try {
      _logger.LogInformation($"Executing {nameof(GetOrder)}");

      var state = GetOrCreateState(sessionId);

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames?.Where(h => h != null).Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname!
        }).ToArray() ?? []
      };

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory!.NewOrder);

      var nonceResult = await GetNonceAsync(sessionId, state.Directory.NewOrder);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
        Url = state.Directory.NewOrder.ToString(),
        Nonce = nonce
      });

      if (!jsonResult.IsSuccess || jsonResult.Value == null)
        return jsonResult;

      var json = jsonResult.Value;

      PrepareRequestContent(request, json, HttpMethod.Post);

      var requestResult = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
      if (!requestResult.IsSuccess || requestResult.Value == null)
        return requestResult;

      var order = requestResult.Value;

      state.CurrentOrder = order.Result;

      return Result.Ok();
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region GetCertificates
  public async Task<Result> GetCertificate(Guid sessionId, string subject) {
    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(GetCertificate)}...");

      if (state.CurrentOrder?.Identifiers == null) {
        return Result.InternalServerError();
      }

      var key = new RSACryptoServiceProvider(4096);
      var csr = new CertificateRequest("CN=" + subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
      var san = new SubjectAlternativeNameBuilder();

      foreach (var host in state.CurrentOrder.Identifiers) {
        if (host?.Value != null)
          san.AddDnsName(host.Value);
      }

      csr.CertificateExtensions.Add(san.Build());

      var letsEncryptOrder = new FinalizeRequest {
        Csr = Base64UrlUtility.Encode(csr.CreateSigningRequest())
      };

      Uri? certificateUrl = default;

      var start = DateTime.UtcNow;

      while (certificateUrl == null) {
        var hostnames = state.CurrentOrder?.Identifiers?.Select(x => x?.Value).Where(x => x != null).Cast<string>().ToArray() ?? [];

        await GetOrder(sessionId, hostnames);

        var status = state.CurrentOrder?.Status;

        if (StatusEquals(status, OrderStatus.Ready)) {
          var request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Finalize);

          var nonceResult = await GetNonceAsync(sessionId, state.CurrentOrder.Finalize);
          if (!nonceResult.IsSuccess || nonceResult.Value == null)
            return nonceResult;

          var nonce = nonceResult.Value;

          var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
            Url = state.CurrentOrder.Finalize.ToString(),
            Nonce = nonce
          });

          if (!jsonResult.IsSuccess || jsonResult.Value == null)
            return jsonResult;

          var json = jsonResult.Value;

          PrepareRequestContent(request, json, HttpMethod.Post);

          var orderResult = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
          if (!orderResult.IsSuccess || orderResult.Value == null)
            return orderResult;

          var order = orderResult.Value;

          if (StatusEquals(order.Result?.Status, OrderStatus.Processing)) {
            request = new HttpRequestMessage(HttpMethod.Post, state.CurrentOrder.Location!);

            nonceResult = await GetNonceAsync(sessionId, state.CurrentOrder.Location);
            if (!nonceResult.IsSuccess || nonceResult.Value == null)
              return nonceResult;

            nonce = nonceResult.Value;

            jsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
              Url = state.CurrentOrder.Location.ToString(),
              Nonce = nonce
            });

            if (!jsonResult.IsSuccess || jsonResult.Value == null)
              return jsonResult;

            json = jsonResult.Value;

            PrepareRequestContent(request, json, HttpMethod.Post);

            orderResult = await SendAcmeRequest<Order>(request, state, HttpMethod.Post);
            if (!orderResult.IsSuccess || orderResult.Value == null)
              return orderResult;

            order = orderResult.Value;
          }

          if (StatusEquals(order.Result?.Status, OrderStatus.Valid)) {
            certificateUrl = order.Result.Certificate;
          }
        }
        else if (StatusEquals(status, OrderStatus.Valid)) {
          certificateUrl = state.CurrentOrder.Certificate;
          break;
        }

        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
      }

      var finalRequest = new HttpRequestMessage(HttpMethod.Post, certificateUrl!);

      var finalNonceResult = await GetNonceAsync(sessionId, certificateUrl);
      if (!finalNonceResult.IsSuccess || finalNonceResult.Value == null)
        return finalNonceResult;

      var finalNonce = finalNonceResult.Value;

      var finalJsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
        Url = certificateUrl.ToString(),
        Nonce = finalNonce
      });

      if (!finalJsonResult.IsSuccess || finalJsonResult.Value == null)
        return finalJsonResult;

      var finalJson = finalJsonResult.Value;

      PrepareRequestContent(finalRequest, finalJson, HttpMethod.Post);

      var requestResult = await SendAcmeRequest<string>(finalRequest, state, HttpMethod.Post);
      if (!requestResult.IsSuccess || requestResult.Value == null)
        return requestResult;

      var pem = requestResult.Value;

      if (state.Cache == null) {
        _logger.LogError($"{nameof(state.Cache)} is null");
        return Result.InternalServerError();
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

      return Result.Ok();
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region Key change
  public Task<Result> KeyChange(Guid sessionId) {
    throw new NotImplementedException();
  }
  #endregion

  #region RevokeCertificate
  public async Task<Result> RevokeCertificate(Guid sessionId, string subject, RevokeReason reason) {
    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(RevokeCertificate)}...");

      if (state.Cache?.CachedCerts == null || !state.Cache.CachedCerts.TryGetValue(subject, out var certificateCache) || certificateCache == null) {
        _logger.LogError("Certificate not found in cache");
        return Result.InternalServerError("Certificate not found");
      }

      var certPem = certificateCache.Cert ?? string.Empty;

      if (string.IsNullOrEmpty(certPem)) {
        _logger.LogError("Certificate PEM is null or empty");
        return Result.InternalServerError("Certificate PEM is null or empty");
      }

      var certificate = new X509Certificate2(Encoding.UTF8.GetBytes(certPem));
      
      var derEncodedCert = certificate.Export(X509ContentType.Cert);
      
      var base64UrlEncodedCert = Base64UrlUtility.Encode(derEncodedCert);
      
      var revokeRequest = new RevokeRequest {
        Certificate = base64UrlEncodedCert,
        Reason = (int)reason
      };

      var request = new HttpRequestMessage(HttpMethod.Post, state.Directory!.RevokeCert);

      var nonceResult = await GetNonceAsync(sessionId, state.Directory.RevokeCert);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var jwsHeader = new ACMEJwsHeader {
        Url = state.Directory.RevokeCert.ToString(),
        Nonce = nonce
      };

      if (!JwsGenerator.TryEncode(state.Rsa, state.Jwk, jwsHeader, revokeRequest, out var jwsMessage, out var errorMessage)) {
        return Result.InternalServerError(errorMessage);
      }

      var json = jwsMessage.ToJson();

      request.Content = new StringContent(json);

      request.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(ContentType.JoseJson));

      var response = await _httpClient.SendAsync(request);

      var responseText = await response.Content.ReadAsStringAsync();

      if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.ProblemJson)) {
        var erroObj = responseText.ToObject<Problem>();
      }

      if (!response.IsSuccessStatusCode)
        Result.InternalServerError(responseText);

      state.Cache.CachedCerts.Remove(subject);
      _logger.LogInformation("Certificate revoked successfully");

      return Result.Ok();

    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion

  #region Internal helpers
  private State GetOrCreateState(Guid sessionId) {
    if (!_memoryCache.TryGetValue(sessionId, out State? state) || state == null) {
      state = new State();
      _memoryCache.Set(sessionId, state, TimeSpan.FromHours(1));
    }
    return state;
  }

  private async Task<Result<string?>> GetNonceAsync(Guid sessionId, Uri uri) {
    if (uri == null)
      return Result<string?>.InternalServerError(null, "URI is null");

    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(GetNonceAsync)}...");

      if (state.Directory?.NewNonce == null)
        return Result<string?>.InternalServerError(null, $"{nameof(state.Directory.NewNonce)} is null");

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, state.Directory.NewNonce));

      var nonce = result.Headers.GetValues("Replay-Nonce").FirstOrDefault();

      if (nonce == null)
        return Result<string?>.InternalServerError(null, "Nonce is null");

      return Result<string?>.Ok(nonce);
    }
    catch (Exception ex) {
      return HandleUnhandledException<string?>(ex);
    }
  }

  // Helper: Send ACME request and process response
  private async Task<Result<SendResult<T>?>> SendAcmeRequest<T>(HttpRequestMessage request, State state, HttpMethod method) {
    try {
      var response = await _httpClient.SendAsync(request);

      var responseText = await response.Content.ReadAsStringAsync();

      HandleProblemResponseAsync(response, responseText);

      var sendResult = ProcessResponseContent<T>(response, responseText);

      return Result<SendResult<T>?>.Ok(sendResult);
    }

    catch (Exception ex) {
      return HandleUnhandledException<SendResult<T>?>(ex);
    }
  }

  private Result<string?> EncodeMessage(Guid sessionId, bool isPostAsGet, object? requestModel, ACMEJwsHeader protectedHeader) {
    var state = GetOrCreateState(sessionId);

    JwsMessage jwsMessage;
    string errorMessage;

    if (isPostAsGet) {
      if (!JwsGenerator.TryEncode(state.Rsa, state.Jwk, protectedHeader, out jwsMessage, out errorMessage))
        return Result<string?>.InternalServerError(errorMessage);

      return Result<string?>.Ok(jwsMessage.ToJson());
    }
    else {
      if (!JwsGenerator.TryEncode(state.Rsa, state.Jwk, protectedHeader, requestModel, out jwsMessage, out errorMessage))
        return Result<string?>.InternalServerError(errorMessage);

      return Result<string?>.Ok(jwsMessage.ToJson());
    }

  }

  private static string GetContentType(ContentType type) => type.GetDisplayName();

  private void PrepareRequestContent(HttpRequestMessage request, string json, HttpMethod method) {
    request.Content = new StringContent(json ?? string.Empty);
    var contentType = method == HttpMethod.Post
      ? GetContentType(ContentType.JoseJson)
      : GetContentType(ContentType.Json);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
  }

  // Helper: Poll challenge status until valid or timeout
  private async Task<Result> PollChallengeStatus(Guid sessionId, AuthorizationChallengeChallenge challenge) {
    if (challenge?.Url == null)
      return Result.InternalServerError("Challenge URL is null");

    var start = DateTime.UtcNow;

    while (true) {
      var pollRequest = new HttpRequestMessage(HttpMethod.Post, challenge.Url);

      var nonceResult = await GetNonceAsync(sessionId, challenge.Url);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var pollJsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
        Url = challenge.Url.ToString(),
        Nonce = nonce
      });

      if (!pollJsonResult.IsSuccess || pollJsonResult.Value == null)
        return pollJsonResult;

      var pollJson = pollJsonResult.Value;

      PrepareRequestContent(pollRequest, pollJson, HttpMethod.Post);

      var pollResponse = await _httpClient.SendAsync(pollRequest);

      var pollResponseText = await pollResponse.Content.ReadAsStringAsync();

      HandleProblemResponseAsync(pollResponse, pollResponseText);

      var authChallenge = ProcessResponseContent<AuthorizationChallengeResponse>(pollResponse, pollResponseText);

      if (authChallenge.Result?.Status != "pending")
        return authChallenge.Result?.Status == "valid" ? Result.Ok() : Result.InternalServerError();

      if ((DateTime.UtcNow - start).Seconds > 120)
        return Result.InternalServerError("Timeout");

      await Task.Delay(1000);
    }
  }

  private void HandleProblemResponseAsync(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.ProblemJson)) {
      var problem = responseText.ToObject<Problem>();

      throw new LetsEncrytException(problem, response);
    }

    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.Json)) {
      var authorizationChallengeChallenge = responseText.ToObject<AuthorizationChallengeChallenge>();

      if (authorizationChallengeChallenge?.Status == "invalid") {
        throw new LetsEncrytException(new Problem {
          Type = authorizationChallengeChallenge.Error.Type,
          Detail = authorizationChallengeChallenge.Error.Detail,
          RawJson = responseText
        }, response);
      }
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

  // Helper for status comparison
  private static bool StatusEquals(string? status, OrderStatus expected) => status == expected.GetDisplayName();

  private Result HandleUnhandledException(Exception ex, string defaultMessage = "Let's Encrypt client unhandled exception") {
    _logger.LogError(ex, defaultMessage);
    return Result.InternalServerError([defaultMessage, .. ex.ExtractMessages()]);
  }

  private Result<T?> HandleUnhandledException<T>(Exception ex, T? defaultValue = default, string defaultMessage = "Let's Encrypt client unhandled exception") {
    _logger.LogError(ex, defaultMessage);
    return Result<T?>.InternalServerError(defaultValue, [.. ex.ExtractMessages()]);
  }
  #endregion
}
