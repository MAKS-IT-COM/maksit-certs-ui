/**
 * https://datatracker.ietf.org/doc/html/rfc8555
 * https://datatracker.ietf.org/doc/html/draft-ietf-acme-acme-12
 */

using MaksIT.Core.Extensions;
using MaksIT.Core.Security;
using MaksIT.Core.Security.JWK;
using MaksIT.Core.Security.JWS;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt.Jws;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Interfaces;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Requests;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace MaksIT.CertsUI.Engine.Services;

public interface ILetsEncryptService {
  Result<RegistrationCache?> GetRegistrationCache(Guid sessionId);
  Task<Result> ConfigureClient(Guid sessionId, bool isStaging);
  Task<Result> Init(Guid sessionId, Guid accountId, string description, string[] contacts, RegistrationCache? registrationCache);
  Result<string?> GetTermsOfServiceUri(Guid sessionId);
  Task<Result<Dictionary<string, string>?>> NewOrder(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> CompleteChallenges(Guid sessionId);
  Task<Result> GetOrder(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificate(Guid sessionId, string subject);
  Task<Result> RevokeCertificate(Guid sessionId, string subject, RevokeReason reason);
}

public partial class LetsEncryptService : ILetsEncryptService {
  private const string DnsType = "dns";
  private const string DirectoryEndpoint = "directory";
  private const string ReplayNonceHeader = "Replay-Nonce";
  private const string AccountKeyMissingMessage = "Account key is not loaded; complete Init before this operation.";

  private readonly ILogger<LetsEncryptService> _logger;
  private readonly ICertsEngineConfiguration _engineConfiguration;
  private readonly HttpClient _httpClient;
  private readonly AcmeSessionStore _sessions;

  public LetsEncryptService(
      ILogger<LetsEncryptService> logger,
      ICertsEngineConfiguration engineConfiguration,
      HttpClient httpClient,
      AcmeSessionStore sessions
   ) {
    _logger = logger;
    _engineConfiguration = engineConfiguration;
    _httpClient = httpClient;
    _sessions = sessions;
  }

  public Result<RegistrationCache?> GetRegistrationCache(Guid sessionId) {
    var state = GetOrCreateState(sessionId);

    if (state.Cache == null)
      return Result<RegistrationCache?>.InternalServerError(null);

    return Result<RegistrationCache?>.Ok(state.Cache);
  }

  #region ConfigureClient
  public async Task<Result> ConfigureClient(Guid sessionId, bool isStaging) {
    try {
      var state = GetOrCreateState(sessionId);

      state.IsStaging = isStaging;

      _httpClient.BaseAddress ??= new Uri(isStaging ? _engineConfiguration.LetsEncryptStaging : _engineConfiguration.LetsEncryptProduction);

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
      var state = GetOrCreateState(sessionId);
      return MapLetsEncryptException(state, ex);
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

        if (state.Directory.NewAccount is not { } newAccountUri)
          return Result.InternalServerError("Directory is missing NewAccount URL.");

        var letsEncryptOrder = new Account {
          TermsOfServiceAgreed = true,
          Contacts = [.. contacts.Select(contact => $"mailto:{contact}")]
        };

        var request = new HttpRequestMessage(HttpMethod.Post, newAccountUri);

        var nonceResult = await GetNonceAsync(sessionId, newAccountUri);
        if (!nonceResult.IsSuccess || nonceResult.Value == null)
          return nonceResult;

        var nonce = nonceResult.Value;

        var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
          Url = newAccountUri.ToString(),
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
          var accountStatusMessage = $"Account status is not valid, was: {result.Result?.Status} \r\n {result.ResponseText}";
          _logger.LogError(accountStatusMessage);
          return Result.InternalServerError(accountStatusMessage);
        }

        state.Cache = new RegistrationCache {
          AccountId = accountId,
          Description = description,
          Contacts = contacts,
          IsStaging = state.IsStaging,
          ChallengeType = ChalengeType.http.GetDisplayName(),
          Location = result.Result.Location,
          AccountKey = accountKey.ExportCspBlob(true),
          AcmeAccountResourceId = result.Result.Id ?? string.Empty,
          Key = result.Result.Key
        };
      }

      return Result.Ok("Initialization successful.");
    }
    catch (LetsEncrytException ex) {
      return MapLetsEncryptException(state, ex);
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

      if (state.Directory?.NewOrder is not { } newOrderUri)
        return Result<Dictionary<string, string>?>.InternalServerError(null);

      var request = new HttpRequestMessage(HttpMethod.Post, newOrderUri);

      var nonceResult = await GetNonceAsync(sessionId, newOrderUri);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

      var nonce = nonceResult.Value;

      var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
        Url = newOrderUri.ToString(),
        Nonce = nonce
      });

      if (!jsonResult.IsSuccess || jsonResult.Value == null)
        return jsonResult.ToResultOfType<Dictionary<string, string>?>(_ => null);

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
          _logger.LogError($"Expected authorization status '{OrderStatus.Pending.GetDisplayName()}', but got: {challengeResponse.Result?.Status} \r\n {challengeResponse.ResponseText}");
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

        if (state.Jwk is null)
          return Result<Dictionary<string, string>?>.InternalServerError(null, AccountKeyMissingMessage);

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

        if (challenge is null) {
          _logger.LogError("Challenge entry is null");
          return Result.InternalServerError("Challenge entry is null");
        }

        if (challenge.Url is null) {
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

        _ = await SendAcmeRequest<AuthorizationChallengeResponse>(request, state, HttpMethod.Post);

        var result = await PollChallengeStatus(sessionId, challenge);

        if (!result.IsSuccess)
          return result;
      }
      return Result.Ok();
    }
    catch (LetsEncrytException ex) {
      return MapLetsEncryptException(GetOrCreateState(sessionId), ex);
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

      if (state.Directory?.NewOrder is not { } newOrderUri)
        return Result.InternalServerError("Directory is not configured. Run ConfigureClient first.");

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames?.Where(h => h != null).Select(hostname => new OrderIdentifier {
          Type = DnsType,
          Value = hostname!
        }).ToArray() ?? []
      };

      var request = new HttpRequestMessage(HttpMethod.Post, newOrderUri);

      var nonceResult = await GetNonceAsync(sessionId, newOrderUri);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
        Url = newOrderUri.ToString(),
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

      if (state.CurrentOrder?.Identifiers is not { } initialIdentifiers)
        return Result.InternalServerError();

      var key = new RSACryptoServiceProvider(4096);
      var csr = new CertificateRequest("CN=" + subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
      var san = new SubjectAlternativeNameBuilder();

      foreach (var host in initialIdentifiers) {
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
        var activeOrder = state.CurrentOrder;
        if (activeOrder?.Identifiers is not { } idents)
          return Result.InternalServerError("Current order identifiers are not available.");

        var hostnames = idents.Select(x => x?.Value).Where(x => x != null).Cast<string>().ToArray();

        await GetOrder(sessionId, hostnames);

        activeOrder = state.CurrentOrder;
        if (activeOrder is null)
          return Result.InternalServerError("Current order is no longer available.");

        var status = activeOrder.Status;

        if (StatusEquals(status, OrderStatus.Ready)) {
          if (activeOrder.Finalize is not { } finalizeUri)
            return Result.InternalServerError("Order finalize URL is missing.");

          var request = new HttpRequestMessage(HttpMethod.Post, finalizeUri);

          var nonceResult = await GetNonceAsync(sessionId, finalizeUri);
          if (!nonceResult.IsSuccess || nonceResult.Value == null)
            return nonceResult;

          var nonce = nonceResult.Value;

          var jsonResult = EncodeMessage(sessionId, false, letsEncryptOrder, new ACMEJwsHeader {
            Url = finalizeUri.ToString(),
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
            activeOrder = state.CurrentOrder;
            if (activeOrder?.Location is not { } orderLocation)
              return Result.InternalServerError("Order location URL is missing.");

            request = new HttpRequestMessage(HttpMethod.Post, orderLocation);

            nonceResult = await GetNonceAsync(sessionId, orderLocation);
            if (!nonceResult.IsSuccess || nonceResult.Value == null)
              return nonceResult;

            nonce = nonceResult.Value;

            jsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
              Url = orderLocation.ToString(),
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
            certificateUrl = order.Result?.Certificate;
            if (certificateUrl is null)
              return Result.InternalServerError("Certificate URL was not returned by the CA.");
          }
        }
        else if (StatusEquals(status, OrderStatus.Valid)) {
          if (activeOrder.Certificate is not { } certUri)
            return Result.InternalServerError("Certificate URL is missing on the order.");
          certificateUrl = certUri;
          break;
        }

        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
      }

      if (certificateUrl is null)
        return Result.InternalServerError("Certificate URL could not be determined.");

      var finalRequest = new HttpRequestMessage(HttpMethod.Post, certificateUrl);

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

      state.Cache.ClearAcmeCooldownForHostname(subject);

      return Result.Ok();
    }
    catch (LetsEncrytException ex) {
      return MapLetsEncryptException(GetOrCreateState(sessionId), ex);
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

      var certificate = X509Certificate2.CreateFromPem(certPem);
      
      var derEncodedCert = certificate.Export(X509ContentType.Cert);
      
      var base64UrlEncodedCert = Base64UrlUtility.Encode(derEncodedCert);
      
      var revokeRequest = new RevokeRequest {
        Certificate = base64UrlEncodedCert,
        Reason = (int)reason
      };

      if (state.Directory?.RevokeCert is not { } revokeUri)
        return Result.InternalServerError("Directory is not configured or RevokeCert URL is missing.");

      if (!state.TryGetAccountKey(out var rsa, out var jwk))
        return Result.InternalServerError(AccountKeyMissingMessage);

      var request = new HttpRequestMessage(HttpMethod.Post, revokeUri);

      var nonceResult = await GetNonceAsync(sessionId, revokeUri);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var jwsHeader = new ACMEJwsHeader {
        Url = revokeUri.ToString(),
        Nonce = nonce
      };

      if (!JwsGenerator.TryEncode(rsa, jwk, jwsHeader, revokeRequest, out var jwsMessage, out var errorMessage)) {
        return Result.InternalServerError(errorMessage);
      }

      var json = jwsMessage.ToJson();

      request.Content = new StringContent(json);

      request.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(ContentType.JoseJson));

      var response = await _httpClient.SendAsync(request);

      var responseText = await response.Content.ReadAsStringAsync();

      HandleProblemResponseAsync(response, responseText);

      try {
        if (!response.IsSuccessStatusCode)
          return Result.InternalServerError(responseText);

        state.Cache.CachedCerts.Remove(subject);
        _logger.LogInformation("Certificate revoked successfully");

        return Result.Ok();
      }
      finally {
        response.Dispose();
      }

    }
    catch (LetsEncrytException ex) {
      var state = GetOrCreateState(sessionId);
      return MapLetsEncryptException(state, ex);
    }
    catch (Exception ex) {
      return HandleUnhandledException(ex);
    }
  }
  #endregion
}
