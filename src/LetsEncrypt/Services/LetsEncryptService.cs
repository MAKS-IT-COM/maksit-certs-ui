/**
* https://community.letsencrypt.org/t/trying-to-do-post-as-get-but-getting-post-jws-not-signed/108371
* https://tools.ietf.org/html/rfc8555#section-6.2
* 
*/
using System.Text;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Logging;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.Core.Extensions;

using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Models.Interfaces;
using MaksIT.LetsEncrypt.Models.Requests;
using MaksIT.LetsEncrypt.Entities.Jws;
using DomainResults.Common;

namespace MaksIT.LetsEncrypt.Services;

public interface ILetsEncryptService {

  Task<IDomainResult> ConfigureClient(string url);

  Task<IDomainResult> Init(string[] contacts, RegistrationCache? registrationCache);

  RegistrationCache? GetRegistrationCache();

  (string?, IDomainResult) GetTermsOfServiceUri();


  Task<(Dictionary<string, string>?, IDomainResult)> NewOrder(string[] hostnames, string challengeType);
  Task<IDomainResult> CompleteChallenges();
  Task<IDomainResult> GetOrder(string[] hostnames);
  Task<((X509Certificate2 Cert, RSA PrivateKey)?, IDomainResult)> GetCertificate(string subject);
}




public class LetsEncryptService : ILetsEncryptService {

  //private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
  //  NullValueHandling = NullValueHandling.Ignore,
  //  Formatting = Formatting.Indented
  //};

  private readonly ILogger<LetsEncryptService> _logger;
    
  private HttpClient _httpClient;

  private IJwsService? _jwsService;
  private AcmeDirectory? _directory;
  private RegistrationCache? _cache;

  private string? _nonce;

  private List<AuthorizationChallenge> _challenges = new List<AuthorizationChallenge>();
  private Order? _currentOrder;

  public LetsEncryptService(
    ILogger<LetsEncryptService> logger,
    HttpClient httpClient
  ) {
    _logger = logger;
    _httpClient = httpClient;
  }


  /// <summary>
  /// 
  /// </summary>
  /// <param name="url"></param>
  /// <param name="contacts"></param>
  /// <returns></returns>
  public async Task<IDomainResult> ConfigureClient(string url) {
    try {
      _httpClient.BaseAddress ??= new Uri(url);

      var (directory, getAcmeDirectoryResult) = await SendAsync<AcmeDirectory>(HttpMethod.Get, new Uri("directory", UriKind.Relative), false, null);
      if (!getAcmeDirectoryResult.IsSuccess)
        return getAcmeDirectoryResult;

      _directory = directory.Result;

      return IDomainResult.Success();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Let's Encrypt client unhandled exception");
      return IDomainResult.CriticalDependencyError();
    }
  }

  /// <summary>
  /// Account creation or Initialization from cache
  /// </summary>
  /// <param name="contacts"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  public async Task<IDomainResult> Init(string? [] contacts, RegistrationCache? cache) {

    try {

      _logger.LogInformation($"Executing {nameof(Init)}...");

      if (contacts == null || contacts.Length == 0)
        return IDomainResult.Failed();

      if (_directory == null)
        return IDomainResult.Failed();

      var accountKey = new RSACryptoServiceProvider(4096);

      if (cache != null && cache.AccountKey != null) {
        _cache = cache;
        accountKey.ImportCspBlob(cache.AccountKey);
      }

      // New Account request
      _jwsService = new JwsService(accountKey);


      var letsEncryptOrder = new Account {
        TermsOfServiceAgreed = true,
        Contacts = contacts.Select(contact => $"mailto:{contact}").ToArray()
      };

      var (account, postAccuntResult) = await SendAsync<Account>(HttpMethod.Post, _directory.NewAccount, false, letsEncryptOrder);
      _jwsService.SetKeyId(account.Result.Location.ToString());

      if (account.Result.Status != "valid") {
        _logger.LogError($"Account status is not valid, was: {account.Result.Status} \r\n {account.ResponseText}");
        return IDomainResult.Failed();
      }

      _cache = new RegistrationCache {
        Location = account.Result.Location,
        AccountKey = accountKey.ExportCspBlob(true),
        Id = account.Result.Id,
        Key = account.Result.Key
      };

      return IDomainResult.Success();
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  public RegistrationCache? GetRegistrationCache() =>
    _cache;

  /// <summary>
  /// Just retrive terms of service
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public (string?, IDomainResult) GetTermsOfServiceUri() {
    try {

      _logger.LogInformation($"Executing {nameof(GetTermsOfServiceUri)}...");

      if (_directory == null) {
        return IDomainResult.Failed<string?>();
      }

      return IDomainResult.Success(_directory.Meta.TermsOfService);
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<string?>(message);
    }
  }

  /// <summary>
  /// Create new Certificate Order. In case you want the wildcard-certificate you must select dns-01 challange.
  /// <para>
  /// Available challange types:
  /// <list type="number">
  /// <item>dns-01</item>
  /// <item>http-01</item>
  /// <item>tls-alpn-01</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="hostnames"></param>
  /// <param name="challengeType"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  public async Task<(Dictionary<string, string>?, IDomainResult)> NewOrder(string[] hostnames, string challengeType) {
    try {

      _logger.LogInformation($"Executing {nameof(NewOrder)}...");

      _challenges.Clear();

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, postNewOrderResult) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, false, letsEncryptOrder);
      if (!postNewOrderResult.IsSuccess) {
        return (null, postNewOrderResult);
      }

      if (order.Result.Status == "ready")
        return IDomainResult.Success(new Dictionary<string, string>());

      if (order.Result.Status != "pending") {
        _logger.LogError($"Created new order and expected status 'pending', but got: {order.Result.Status} \r\n {order.Result}");
        return IDomainResult.Failed<Dictionary<string, string>?>();
      }

      _currentOrder = order.Result;

      var results = new Dictionary<string, string>();
      foreach (var item in order.Result.Authorizations) {

        var (challengeResponse, postAuthorisationChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, item, true, null);
        if (!postAuthorisationChallengeResult.IsSuccess) {
          return (null, postAuthorisationChallengeResult);
        }

        if (challengeResponse.Result.Status == "valid")
          continue;

        if (challengeResponse.Result.Status != "pending") {
          _logger.LogError($"Expected autorization status 'pending', but got: {order.Result.Status} \r\n {challengeResponse.ResponseText}");
          return IDomainResult.Failed<Dictionary<string, string>?>();
        }

        var challenge = challengeResponse.Result.Challenges.First(x => x.Type == challengeType);
        _challenges.Add(challenge);

        var keyToken = _jwsService.GetKeyAuthorization(challenge.Token);

        switch (challengeType) {

          // A client fulfills this challenge by constructing a key authorization
          // from the "token" value provided in the challenge and the client's
          // account key.  The client then computes the SHA-256 digest [FIPS180-4]
          // of the key authorization.
          // 
          // The record provisioned to the DNS contains the base64url encoding of
          // this digest.

          case "dns-01": {
              using (var sha256 = SHA256.Create()) {
                var dnsToken = _jwsService.Base64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyToken)));
                results[challengeResponse.Result.Identifier.Value] = dnsToken;
              }
              break;
            }


          // A client fulfills this challenge by constructing a key authorization
          // from the "token" value provided in the challenge and the client's
          // account key.  The client then provisions the key authorization as a
          // resource on the HTTP server for the domain in question.
          // 
          // The path at which the resource is provisioned is comprised of the
          // fixed prefix "/.well-known/acme-challenge/", followed by the "token"
          // value in the challenge.  The value of the resource MUST be the ASCII
          // representation of the key authorization.

          case "http-01": {
              results[challengeResponse.Result.Identifier.Value] = keyToken;
              break;
            }

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

  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public async Task<IDomainResult> CompleteChallenges() {
    try {

      _logger.LogInformation($"Executing {nameof(CompleteChallenges)}...");

      if (_currentOrder?.Identifiers == null) {
        return IDomainResult.Failed();
      }

      for (var index = 0; index < _challenges.Count; index++) {

        var challenge = _challenges[index];

        var start = DateTime.UtcNow;

        while (true) {
          var authorizeChallenge = new AuthorizeChallenge();

          switch (challenge.Type) {
            case "dns-01": {
                authorizeChallenge.KeyAuthorization = _jwsService.GetKeyAuthorization(challenge.Token);
                //var (result, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, authorizeChallenge, token);
                break;
              }

            case "http-01": {
                break;
              }
          }

          var (authChallenge, postAuthChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, false, "{}");
          if (!postAuthChallengeResult.IsSuccess) {
            return postAuthChallengeResult;
          }

          if (authChallenge.Result.Status == "valid")
            break;

          if (authChallenge.Result.Status != "pending") {
            _logger.LogError($"Failed autorization of {_currentOrder.Identifiers[index].Value} \r\n {authChallenge.ResponseText}");
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

  /// <summary>
  /// 
  /// </summary>
  /// <param name="hostnames"></param>
  /// <returns></returns>
  public async Task<IDomainResult> GetOrder(string[] hostnames) {

    try {

      _logger.LogInformation($"Executing {nameof(GetOrder)}");

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, false, letsEncryptOrder);
      if (!postOrderResult.IsSuccess)
        return postOrderResult;

      _currentOrder = order.Result;

      return IDomainResult.Success();
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError(message);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="subject"></param>
  /// <returns>Cert and Private key</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public async Task<((X509Certificate2 Cert, RSA PrivateKey)?, IDomainResult)> GetCertificate(string subject) {

    try {
      _logger.LogInformation($"Executing {nameof(GetCertificate)}...");

      if (_currentOrder == null) {
        return IDomainResult.Failed<(X509Certificate2 Cert, RSA PrivateKey)?>();
      }

      var key = new RSACryptoServiceProvider(4096);
      var csr = new CertificateRequest("CN=" + subject,
          key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

      var san = new SubjectAlternativeNameBuilder();
      foreach (var host in _currentOrder.Identifiers)
        san.AddDnsName(host.Value);

      csr.CertificateExtensions.Add(san.Build());

      var letsEncryptOrder = new FinalizeRequest {
        Csr = _jwsService.Base64UrlEncoded(csr.CreateSigningRequest())
      };

      Uri? certificateUrl = default;


      var start = DateTime.UtcNow;

      while (certificateUrl == null) {
        // https://community.letsencrypt.org/t/breaking-changes-in-asynchronous-order-finalization-api/195882
        await GetOrder(_currentOrder.Identifiers.Select(x => x.Value).ToArray());

        if (_currentOrder.Status == "ready") {
          var (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, _currentOrder.Finalize, false, letsEncryptOrder);
          if (!postOrderResult.IsSuccess || order?.Result == null)
            return (null, postOrderResult);


          if (order.Result.Status == "processing") {
            (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, _currentOrder.Location, true, null);
            if (!postOrderResult.IsSuccess || order?.Result == null)
              return (null, postOrderResult);
          }

          if (order.Result.Status == "valid") {
            certificateUrl = order.Result.Certificate;
          }
        }

        if ((DateTime.UtcNow - start).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
      }

      var (pem, postPemResult) = await SendAsync<string>(HttpMethod.Post, certificateUrl, true, null);
      if (!postPemResult.IsSuccess || pem?.Result == null)
        return (null, postPemResult);


      if (_cache == null) {
        _logger.LogError($"{nameof(_cache)} is null");
        return IDomainResult.Failed<(X509Certificate2 Cert, RSA PrivateKey)?>();
      }

      _cache.CachedCerts ??= new Dictionary<string, CertificateCache>();
      _cache.CachedCerts[subject] = new CertificateCache {
        Cert = pem.Result,
        Private = key.ExportCspBlob(true)
      };

      var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pem.Result));

      return IDomainResult.Success((cert, key));
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError< (X509Certificate2 Cert, RSA PrivateKey)?>(message);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public Task<IDomainResult> KeyChange() {
    throw new NotImplementedException();
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public Task<IDomainResult> RevokeCertificate() {
    throw new NotImplementedException();
  }


  /// <summary>
  /// Request New Nonce to be able to start POST requests
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  private async Task<(string?, IDomainResult)> NewNonce() {

    try {

      _logger.LogInformation($"Executing {nameof(NewNonce)}...");

      if (_directory == null)
        IDomainResult.Failed();

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, _directory.NewNonce));
      return IDomainResult.Success(result.Headers.GetValues("Replay-Nonce").First());

    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<string?>(message);
    }
  }

  /// <summary>
  /// Main method used to send data to LetsEncrypt
  /// </summary>
  /// <typeparam name="TResult"></typeparam>
  /// <param name="method"></param>
  /// <param name="uri"></param>
  /// <param name="requestModel"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  private async Task<(SendResult<TResult>?, IDomainResult)> SendAsync<TResult>(HttpMethod method, Uri uri, bool isPostAsGet, object? requestModel) {
    try {

      _logger.LogInformation($"Executing {nameof(SendAsync)}...");

      //if (_jwsService == null) {
      //  _logger.LogError($"{nameof(_jwsService)} is null");
      //  return IDomainResult.Failed<SendResult<TResult>?>();
      //}

      var request = new HttpRequestMessage(method, uri);

      if (uri.OriginalString != "directory") {
        var (nonce, newNonceResult) = await NewNonce();
        if (!newNonceResult.IsSuccess || nonce == null) {
          return (null, newNonceResult);
        }

        _nonce = nonce;
      }
      else {
        _nonce = default;
      }

      if (requestModel != null || isPostAsGet) {
        var jwsHeader = new JwsHeader {
          Url = uri,
        };

        if (_nonce != null)
          jwsHeader.Nonce = _nonce;

        var encodedMessage = isPostAsGet
          ? _jwsService.Encode(jwsHeader)
          : _jwsService.Encode(requestModel, jwsHeader);

        var json = encodedMessage.ToJson();

        request.Content = new StringContent(json);

        var requestType = "application/json";
        if (method == HttpMethod.Post)
          requestType = "application/jose+json";

        request.Content.Headers.Remove("Content-Type");
        request.Content.Headers.Add("Content-Type", requestType);
      }

      var response = await _httpClient.SendAsync(request);

      if (method == HttpMethod.Post)
        _nonce = response.Headers.GetValues("Replay-Nonce").First();

      var responseText = await response.Content.ReadAsStringAsync();

      if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
        throw new LetsEncrytException(responseText.ToObject<Problem>(), response);

      if (response.Content.Headers.ContentType?.MediaType == "application/pem-certificate-chain" && typeof(TResult) == typeof(string)) {
        return IDomainResult.Success(new SendResult<TResult> {
          Result = (TResult)(object)responseText
        });
      }

      var responseContent = responseText.ToObject<TResult>();

      if (responseContent is IHasLocation ihl) {
        if (response.Headers.Location != null)
          ihl.Location = response.Headers.Location;
      }

      return IDomainResult.Success(new SendResult<TResult> {
        Result = responseContent,
        ResponseText = responseText
      });

    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<SendResult<TResult>?>(message);
    }
  }
}
