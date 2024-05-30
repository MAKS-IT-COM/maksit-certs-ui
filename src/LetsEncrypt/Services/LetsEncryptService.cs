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
  Task<(AcmeDirectory?, IDomainResult)> ConfigureClient(string url);
  Task<(RegistrationCache?, IDomainResult)> Init(Uri newAccount, Uri newNonce, string[] contacts);
  Task<((Order?, Dictionary<string, string>?, List<AuthorizationChallenge>?), IDomainResult)> NewOrder(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, string location, string[] hostnames, string challengeType);
  Task<IDomainResult> CompleteChallenges(Uri newNonce, byte[] accountKeyBytes, string location, Order currentOrder, List<AuthorizationChallenge> _challenges);
  Task<(Order?, IDomainResult)> GetOrder(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, string location, string[] hostnames);
  Task<(Dictionary<string, CertificateCache>?, IDomainResult)> GetCertificate(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, Order currentOrder, string location, string [] subjects);
}

public class LetsEncryptService : ILetsEncryptService {

  private readonly ILogger<LetsEncryptService> _logger;
  private readonly HttpClient _httpClient;

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
  public async Task<(AcmeDirectory?, IDomainResult)> ConfigureClient(string url) {
    try {
      _httpClient.BaseAddress ??= new Uri(url);

      var (directory, getAcmeDirectoryResult) = await SendAsync<AcmeDirectory>(HttpMethod.Get, new Uri("directory", UriKind.Relative), false, null, null, null, null);
      if (!getAcmeDirectoryResult.IsSuccess)
        return (null, getAcmeDirectoryResult);

      var result = directory?.Result;

      return IDomainResult.Success(result);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Let's Encrypt client unhandled exception");
      return IDomainResult.CriticalDependencyError<AcmeDirectory>();
    }
  }

  /// <summary>
  /// Account creation or Initialization from cache
  /// </summary>
  /// <param name="contacts"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  public async Task<(RegistrationCache?, IDomainResult)> Init(Uri newAccount, Uri newNonce, string[] contacts) {

    try {

      _logger.LogInformation($"Executing {nameof(Init)}...");

      var accountKey = new RSACryptoServiceProvider(4096);
      var jwsService = new JwsService(accountKey);


      var letsEncryptOrder = new Account {
        TermsOfServiceAgreed = true,
        Contacts = contacts.Select(contact => $"mailto:{contact}").ToArray()
      };

      var (account, postAccuntResult) = await SendAsync<Account>(HttpMethod.Post, newAccount, false, letsEncryptOrder, accountKey, null, newNonce);
      if (!postAccuntResult.IsSuccess || account == null)
        return (null, postAccuntResult);

      // Probably non necessary here
      // jwsService.SetKeyId(account.Result.Location.ToString());

      if (account.Result.Status != "valid") {
        _logger.LogError($"Account status is not valid, was: {account.Result.Status} \r\n {account.ResponseText}");
        return IDomainResult.Failed<RegistrationCache>();
      }

      var cache = new RegistrationCache {
        Location = account.Result.Location,
        AccountKey = accountKey.ExportCspBlob(true),
        Id = account.Result.Id,
        Key = account.Result.Key
      };

      return IDomainResult.Success(cache);
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<RegistrationCache>(message);
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
  public async Task<((Order?, Dictionary<string, string>?, List<AuthorizationChallenge>?), IDomainResult)> NewOrder(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, string location, string[] hostnames, string challengeType) {
    try {

      var accountKey = new RSACryptoServiceProvider(4096);
      accountKey.ImportCspBlob(accountKeyBytes);

      var jwsService = new JwsService(accountKey);

      _logger.LogInformation($"Executing {nameof(NewOrder)}...");

      var currentOrder = default(Order);
      var results = new Dictionary<string, string>();
      var challenges = new List<AuthorizationChallenge>();

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, postNewOrderResult) = await SendAsync<Order>(HttpMethod.Post, newOrder, false, letsEncryptOrder, accountKey, location, newNonce);
      if (!postNewOrderResult.IsSuccess) {
        return ((null, null, null), postNewOrderResult);
      }

      if (order.Result.Status == "ready")
        return IDomainResult.Success((currentOrder, results, challenges));

      if (order.Result.Status != "pending") {
        _logger.LogError($"Created new order and expected status 'pending', but got: {order.Result.Status} \r\n {order.Result}");
        return IDomainResult.Failed<(Order?, Dictionary<string, string>?, List<AuthorizationChallenge>?)>();
      }

      currentOrder = order.Result;

      
      foreach (var item in currentOrder.Authorizations) {

        var (challengeResponse, postAuthorisationChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, item, true, null, accountKey, location, newNonce);
        if (!postAuthorisationChallengeResult.IsSuccess) {
          return ((null, null, null), postAuthorisationChallengeResult);
        }

        if (challengeResponse.Result.Status == "valid")
          continue;

        if (challengeResponse.Result.Status != "pending") {
          _logger.LogError($"Expected autorization status 'pending', but got: {currentOrder.Status} \r\n {challengeResponse.ResponseText}");
          return IDomainResult.Failed<(Order?, Dictionary<string, string>?, List<AuthorizationChallenge>?)>();
        }

        var challenge = challengeResponse.Result.Challenges.First(x => x.Type == challengeType);
        challenges.Add(challenge);

        var keyToken = jwsService.GetKeyAuthorization(challenge.Token);

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
                var dnsToken = jwsService.Base64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(keyToken)));
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

      // TODO: reurn challenges
      return IDomainResult.Success((currentOrder, results, challenges));
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<(Order?, Dictionary<string, string>?, List<AuthorizationChallenge>?)>(message);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public async Task<IDomainResult> CompleteChallenges(Uri newNonce, byte[] accountKeyBytes, string location, Order currentOrder, List<AuthorizationChallenge> challenges) {
    try {

      var accountKey = new RSACryptoServiceProvider(4096);
      accountKey.ImportCspBlob(accountKeyBytes);
      var jwsService = new JwsService(accountKey);

      _logger.LogInformation($"Executing {nameof(CompleteChallenges)}...");

      if (currentOrder?.Identifiers == null) {
        return IDomainResult.Failed();
      }

      for (var index = 0; index < challenges.Count; index++) {

        var challenge = challenges[index];

        var start = DateTime.UtcNow;

        while (true) {
          var authorizeChallenge = new AuthorizeChallenge();

          switch (challenge.Type) {
            case "dns-01": {
                authorizeChallenge.KeyAuthorization = jwsService.GetKeyAuthorization(challenge.Token);
                //var (result, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, authorizeChallenge, token);
                break;
              }

            case "http-01": {
                break;
              }
          }

          var (authChallenge, postAuthChallengeResult) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, false, "{}", accountKey, location, newNonce);
          if (!postAuthChallengeResult.IsSuccess) {
            return postAuthChallengeResult;
          }

          if (authChallenge.Result.Status == "valid")
            break;

          if (authChallenge.Result.Status != "pending") {
            _logger.LogError($"Failed autorization of {currentOrder.Identifiers[index].Value} \r\n {authChallenge.ResponseText}");
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
  public async Task<(Order?, IDomainResult)> GetOrder(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, string location, string[] hostnames) {

    try {
      var accountKey = new RSACryptoServiceProvider(4096);
      accountKey.ImportCspBlob(accountKeyBytes);

      _logger.LogInformation($"Executing {nameof(GetOrder)}");

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, newOrder, false, letsEncryptOrder, accountKey, location, newNonce);
      if (!postOrderResult.IsSuccess)
        return (null, postOrderResult);

      var currentOrder = order.Result;

      return IDomainResult.Success(currentOrder);
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<Order?>(message);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="subject"></param>
  /// <returns>Cert and Private key</returns>
  /// <exception cref="InvalidOperationException"></exception>
  public async Task<(Dictionary<string, CertificateCache>?, IDomainResult)> GetCertificate(Uri newOrder, Uri newNonce, byte[] accountKeyBytes, Order currentOrder, string location, string [] subjects) {

    try {

      var accountKey = new RSACryptoServiceProvider(4096);
      accountKey.ImportCspBlob(accountKeyBytes);

      var jwsService = new JwsService(accountKey);


      _logger.LogInformation($"Executing {nameof(GetCertificate)}...");

      var cachedCerts = new Dictionary<string, CertificateCache>();


      foreach (var subject in subjects) {


        if (currentOrder == null) {
          return IDomainResult.Failed<Dictionary<string, CertificateCache>>();
        }

        var key = new RSACryptoServiceProvider(4096);
        var csr = new CertificateRequest("CN=" + subject,
            key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        foreach (var host in currentOrder.Identifiers)
          san.AddDnsName(host.Value);

        csr.CertificateExtensions.Add(san.Build());

        var letsEncryptOrder = new FinalizeRequest {
          Csr = jwsService.Base64UrlEncoded(csr.CreateSigningRequest())
        };

        Uri? certificateUrl = default;


        var start = DateTime.UtcNow;

        while (certificateUrl == null) {
          // https://community.letsencrypt.org/t/breaking-changes-in-asynchronous-order-finalization-api/195882
          await GetOrder(newOrder, newNonce, accountKeyBytes, location, currentOrder.Identifiers.Select(x => x.Value).ToArray());

          if (currentOrder.Status == "ready") {
            var (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, currentOrder.Finalize, false, letsEncryptOrder, accountKey, location, newNonce);
            if (!postOrderResult.IsSuccess || order?.Result == null)
              return (null, postOrderResult);


            if (order.Result.Status == "processing") {
              (order, postOrderResult) = await SendAsync<Order>(HttpMethod.Post, currentOrder.Location, true, null, accountKey, location, newNonce);
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

        var (pem, postPemResult) = await SendAsync<string>(HttpMethod.Post, certificateUrl, true, null, accountKey, location, newNonce);
        if (!postPemResult.IsSuccess || pem?.Result == null)
          return (null, postPemResult);



        cachedCerts.Add(subject, new CertificateCache {
          Cert = pem.Result,
          Private = key.ExportCspBlob(true)
        });

        //var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pem.Result));

      }

      return IDomainResult.Success(cachedCerts);
    }
    catch (Exception ex) {
      var message = "Let's Encrypt client unhandled exception";

      _logger.LogError(ex, message);
      return IDomainResult.CriticalDependencyError<Dictionary<string, CertificateCache>?>(message);
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
  private async Task<(string?, IDomainResult)> NewNonce(Uri newNonce) {

    try {

      _logger.LogInformation($"Executing {nameof(NewNonce)}...");

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, newNonce));
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
  private async Task<(SendResult<TResult>?, IDomainResult)> SendAsync<TResult>(HttpMethod method, Uri uri, bool isPostAsGet, object? requestModel, RSACryptoServiceProvider? accountKey, string? location, Uri? newNonce) {
    try {
      var _nonce = default(string?);

      _logger.LogInformation($"Executing {nameof(SendAsync)}...");

      var request = new HttpRequestMessage(method, uri);

      if (uri.OriginalString != "directory") {
        var (nonce, newNonceResult) = await NewNonce(newNonce);
        if (!newNonceResult.IsSuccess || nonce == null) {
          return (null, newNonceResult);
        }

        _nonce = nonce;
      }

      if (requestModel != null || isPostAsGet) {

        if (accountKey == null)
          return IDomainResult.Failed<SendResult<TResult>?>();

        var jwsService = new JwsService(accountKey);
        if(location != null)
          jwsService.SetKeyId(location);

        var jwsHeader = new JwsHeader {
          Url = uri,
        };

        if (_nonce != null)
          jwsHeader.Nonce = _nonce;

        var encodedMessage = isPostAsGet
          ? jwsService.Encode(jwsHeader)
          : jwsService.Encode(requestModel, jwsHeader);

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
