using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.Core.Extensions;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Models.Interfaces;
using MaksIT.LetsEncrypt.Models.Requests;
using MaksIT.LetsEncrypt.Entities.Jws;
using DomainResults.Common;
using System.Net.Http.Headers;

namespace MaksIT.LetsEncrypt.Services;

public interface ILetsEncryptService {
  Task<IDomainResult> ConfigureClient(Guid sessionId, string url);
  Task<IDomainResult> Init(Guid sessionId,Guid accountId, string[] contacts, RegistrationCache? registrationCache);
  (RegistrationCache?, IDomainResult) GetRegistrationCache(Guid sessionId);
  (string?, IDomainResult) GetTermsOfServiceUri(Guid sessionId);
  Task<(Dictionary<string, string>?, IDomainResult)> NewOrder(Guid sessionId, string[] hostnames, string challengeType);
  Task<IDomainResult> CompleteChallenges(Guid sessionId);
  Task<IDomainResult> GetOrder(Guid sessionId, string[] hostnames);
  Task<IDomainResult> GetCertificate(Guid sessionId, string subject);
  (CachedCertificateResult?, IDomainResult) TryGetCachedCertificate(Guid sessionId, string subject);
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
  public async Task<IDomainResult> ConfigureClient(Guid sessionId, string url) {
    try {
      var state = GetOrCreateState(sessionId);

      _httpClient.BaseAddress ??= new Uri(url);

      if (state.Directory == null) {
        var (directory, getAcmeDirectoryResult) = await SendAsync<AcmeDirectory>(sessionId, HttpMethod.Get, new Uri("directory", UriKind.Relative), false, null);
        if (!getAcmeDirectoryResult.IsSuccess || directory == null)
          return getAcmeDirectoryResult;

        state.Directory = directory.Result;
      }

      return IDomainResult.Success();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Let's Encrypt client unhandled exception");
      return IDomainResult.CriticalDependencyError();
    }
  }
  #endregion

  #region Init
  public async Task<IDomainResult> Init(Guid sessionId, Guid accountId, string[] contacts, RegistrationCache? cache) {
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
      }
      else {
        // New Account request
        state.JwsService = new JwsService(accountKey);

        var letsEncryptOrder = new Account {
          TermsOfServiceAgreed = true,
          Contacts = contacts.Select(contact => $"mailto:{contact}").ToArray()
        };

        var (account, postAccountResult) = await SendAsync<Account>(sessionId, HttpMethod.Post, state.Directory.NewAccount, false, letsEncryptOrder);
        state.JwsService.SetKeyId(account.Result.Location.ToString());

        if (account.Result.Status != "valid") {
          _logger.LogError($"Account status is not valid, was: {account.Result.Status} \r\n {account.ResponseText}");
          return IDomainResult.Failed();
        }

        state.Cache = new RegistrationCache {
          AccountId = accountId,

          Location = account.Result.Location,
          AccountKey = accountKey.ExportCspBlob(true),
          Id = account.Result.Id,
          Key = account.Result.Key
        };
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

      var (order, postNewOrderResult) = await SendAsync<Order>(sessionId, HttpMethod.Post, state.Directory.NewOrder, false, letsEncryptOrder);
      if (!postNewOrderResult.IsSuccess) {
        return (null, postNewOrderResult);
      }

      if (order.Result.Status == "ready")
        return IDomainResult.Success(new Dictionary<string, string>());

      if (order.Result.Status != "pending") {
        _logger.LogError($"Created new order and expected status 'pending', but got: {order.Result.Status} \r\n {order.Result}");
        return IDomainResult.Failed<Dictionary<string, string>?>();
      }

      state.CurrentOrder = order.Result;

      var results = new Dictionary<string, string>();
      foreach (var item in order.Result.Authorizations) {
        var (challengeResponse, postAuthorisationChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(sessionId, HttpMethod.Post, item, true, null);
        if (!postAuthorisationChallengeResult.IsSuccess) {
          return (null, postAuthorisationChallengeResult);
        }

        if (challengeResponse.Result.Status == "valid")
          continue;

        if (challengeResponse.Result.Status != "pending") {
          _logger.LogError($"Expected authorization status 'pending', but got: {order.Result.Status} \r\n {challengeResponse.ResponseText}");
          return IDomainResult.Failed<Dictionary<string, string>?>();
        }

        var challenge = challengeResponse.Result.Challenges.First(x => x.Type == challengeType);
        state.Challenges.Add(challenge);

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
        return IDomainResult.Failed();
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

          var (authChallenge, postAuthChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(sessionId, HttpMethod.Post, challenge.Url, false, "{}");
          if (!postAuthChallengeResult.IsSuccess) {
            return postAuthChallengeResult;
          }

          if (authChallenge.Result.Status == "valid")
            break;

          if (authChallenge.Result.Status != "pending") {
            _logger.LogError($"Challenge failed with status {authChallenge.Result.Status} \r\n {authChallenge.ResponseText}");
            return IDomainResult.Failed();
          }

          await Task.Delay(1000);

          if ((DateTime.UtcNow - start).Seconds > 120)
            throw new TimeoutException();
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

      var (order, postOrderResult) = await SendAsync<Order>(sessionId, HttpMethod.Post, state.Directory.NewOrder, false, letsEncryptOrder);
      if (!postOrderResult.IsSuccess)
        return postOrderResult;

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
          var (order, postOrderResult) = await SendAsync<Order>(sessionId, HttpMethod.Post, state.CurrentOrder.Finalize, false, letsEncryptOrder);
          if (!postOrderResult.IsSuccess || order?.Result == null)
            return postOrderResult;

          if (order.Result.Status == "processing") {
            (order, postOrderResult) = await SendAsync<Order>(sessionId, HttpMethod.Post, state.CurrentOrder.Location, true, null);
            if (!postOrderResult.IsSuccess || order?.Result == null)
              return postOrderResult;
          }

          if (order.Result.Status == "valid") {
            certificateUrl = order.Result.Certificate;
          }
        }

        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
      }     

      var (pem, postPemResult) = await SendAsync<string>(sessionId, HttpMethod.Post, certificateUrl, true, null);
      if (!postPemResult.IsSuccess || pem?.Result == null)
        return postPemResult;

      if (state.Cache == null) {
        _logger.LogError($"{nameof(state.Cache)} is null");
        return IDomainResult.Failed();
      }

      state.Cache.CachedCerts ??= new Dictionary<string, CertificateCache>();
      state.Cache.CachedCerts[subject] = new CertificateCache {
        Cert = pem.Result,
        Private = key.ExportCspBlob(true)
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

  #region TryGetCachedCertificate
  public (CachedCertificateResult?, IDomainResult) TryGetCachedCertificate(Guid sessionId, string subject) {

    var state = GetOrCreateState(sessionId);
    
    var certRes = new CachedCertificateResult();
    if (state.Cache != null && state.Cache.TryGetCachedCertificate(subject, out certRes)) {
      return IDomainResult.Success(certRes);
    }

    return IDomainResult.Failed<CachedCertificateResult?>();
  }
  #endregion


  public Task<IDomainResult> KeyChange(Guid sessionId) {
    throw new NotImplementedException();
  }

  public Task<IDomainResult> RevokeCertificate(Guid sessionId) {
    throw new NotImplementedException();
  }

  #region SendAsync
  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="TResult"></typeparam>
  /// <param name="sessionId"></param>
  /// <param name="method"></param>
  /// <param name="uri"></param>
  /// <param name="isPostAsGet"></param>
  /// <param name="requestModel"></param>
  /// <returns></returns>
  //private async Task<(SendResult<TResult>?, IDomainResult)> SendAsync<TResult>(Guid sessionId, HttpMethod method, Uri uri, bool isPostAsGet, object? requestModel) {
  //  try {
  //    var state = GetOrCreateState(sessionId);

  //    _logger.LogInformation($"Executing {nameof(SendAsync)}...");

  //    var request = new HttpRequestMessage(method, uri);

  //    if (uri.OriginalString != "directory") {
  //      var (nonce, newNonceResult) = await NewNonce(sessionId);
  //      if (!newNonceResult.IsSuccess || nonce == null) {
  //        return (null, newNonceResult);
  //      }

  //      state.Nonce = nonce;
  //    }
  //    else {
  //      state.Nonce = default;
  //    }

  //    if (requestModel != null || isPostAsGet) {
  //      var jwsHeader = new JwsHeader {
  //        Url = uri,
  //      };

  //      if (state.Nonce != null)
  //        jwsHeader.Nonce = state.Nonce;

  //      var encodedMessage = isPostAsGet
  //          ? state.JwsService.Encode(jwsHeader)
  //          : state.JwsService.Encode(requestModel, jwsHeader);

  //      var json = encodedMessage.ToJson();

  //      request.Content = new StringContent(json);

  //      var requestType = "application/json";
  //      if (method == HttpMethod.Post)
  //        requestType = "application/jose+json";

  //      request.Content.Headers.Remove("Content-Type");
  //      request.Content.Headers.Add("Content-Type", requestType);
  //    }

  //    var response = await _httpClient.SendAsync(request);

  //    if (method == HttpMethod.Post)
  //      state.Nonce = response.Headers.GetValues("Replay-Nonce").First();

  //    var responseText = await response.Content.ReadAsStringAsync();

  //    if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
  //      throw new LetsEncrytException(responseText.ToObject<Problem>(), response);

  //    if (response.Content.Headers.ContentType?.MediaType == "application/pem-certificate-chain" && typeof(TResult) == typeof(string)) {
  //      return IDomainResult.Success(new SendResult<TResult> {
  //        Result = (TResult)(object)responseText
  //      });
  //    }

  //    var responseContent = responseText.ToObject<TResult>();

  //    if (responseContent is IHasLocation ihl) {
  //      if (response.Headers.Location != null)
  //        ihl.Location = response.Headers.Location;
  //    }

  //    return IDomainResult.Success(new SendResult<TResult> {
  //      Result = responseContent,
  //      ResponseText = responseText
  //    });

  //  }
  //  catch (Exception ex) {
  //    var message = "Let's Encrypt client unhandled exception";

  //    _logger.LogError(ex, message);
  //    return IDomainResult.CriticalDependencyError<SendResult<TResult>?>(message);
  //  }
  //}

  private async Task<(SendResult<TResult>?, IDomainResult)> SendAsync<TResult>(
    Guid sessionId,
    HttpMethod method,
    Uri uri,
    bool isPostAsGet,
    object? requestModel
  ) {
    try {
      var state = GetOrCreateState(sessionId);
      _logger.LogInformation($"Executing {nameof(SendAsync)}...");

      var request = new HttpRequestMessage(method, uri);
      await HandleNonceAsync(sessionId, uri, state);

      if (requestModel != null || isPostAsGet) {
        var jwsHeader = CreateJwsHeader(uri, state.Nonce);
        var json = EncodeMessage(isPostAsGet, requestModel, state, jwsHeader);
        PrepareRequestContent(request, json, method);
      }

      var response = await _httpClient.SendAsync(request);
      await UpdateStateNonceIfNeededAsync(response, state, method);

      var responseText = await response.Content.ReadAsStringAsync();
      await HandleProblemResponseAsync(response, responseText);

      var result = ProcessResponseContent<TResult>(response, responseText);
      return IDomainResult.Success(result);
    }
    catch (Exception ex) {
      const string message = "Let's Encrypt client unhandled exception";
      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<SendResult<TResult>?>(message);
    }
  }

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

  private JwsHeader CreateJwsHeader(Uri uri, string? nonce) {
    return new JwsHeader {
      Url = uri,
      Nonce = nonce
    };
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

  private async Task UpdateStateNonceIfNeededAsync(HttpResponseMessage response, State state, HttpMethod method) {
    if (method == HttpMethod.Post && response.Headers.Contains("Replay-Nonce")) {
      state.Nonce = response.Headers.GetValues("Replay-Nonce").First();
    }
  }

  private async Task HandleProblemResponseAsync(HttpResponseMessage response, string responseText) {
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

  private class State {
    public AcmeDirectory? Directory { get; set; }
    public JwsService? JwsService { get; set; }
    public Order? CurrentOrder { get; set; }
    public List<AuthorizationChallenge> Challenges { get; } = new List<AuthorizationChallenge>();
    public string? Nonce { get; set; }
    public RegistrationCache? Cache { get; set; }
  }
}
