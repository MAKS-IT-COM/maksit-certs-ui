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
using System.Xml;
using System.Diagnostics;

namespace MaksIT.LetsEncrypt.Services {

  public interface ILetsEncryptService {

    Task ConfigureClient(string url);

    Task Init(string[] contacts, RegistrationCache? registrationCache);

    string GetTermsOfServiceUri();


    Task<Dictionary<string, string>> NewOrder(string[] hostnames, string challengeType);
    Task CompleteChallenges();
    Task GetOrder(string[] hostnames);
    Task<(X509Certificate2 Cert, RSA PrivateKey)> GetCertificate(string subject);

    RegistrationCache? GetRegistrationCache();
  }




  public class LetsEncryptService : ILetsEncryptService {

    //private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
    //  NullValueHandling = NullValueHandling.Ignore,
    //  Formatting = Formatting.Indented
    //};

    private readonly ILogger<LetsEncryptService> _logger;
    
    private HttpClient _httpClient;

    private IJwsService _jwsService;
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
    public async Task ConfigureClient(string url) {

      _httpClient.BaseAddress ??= new Uri(url);

      (_directory, _) = await SendAsync<AcmeDirectory>(HttpMethod.Get, new Uri("directory", UriKind.Relative), false, null);
    }

    /// <summary>
    /// Account creation or Initialization from cache
    /// </summary>
    /// <param name="contacts"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task Init(string? [] contacts, RegistrationCache? cache) {

      if (contacts == null || contacts.Length == 0)
        throw new ArgumentNullException();

      if (_directory == null)
        throw new ArgumentNullException();

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

      var (account, response) = await SendAsync<Account>(HttpMethod.Post, _directory.NewAccount, false, letsEncryptOrder);
      _jwsService.SetKeyId(account.Location.ToString());

      if (account.Status != "valid")
        throw new InvalidOperationException($"Account status is not valid, was: {account.Status} \r\n {response}");

      _cache = new RegistrationCache {
        Location = account.Location,
        AccountKey = accountKey.ExportCspBlob(true),
        Id = account.Id,
        Key = account.Key
      };
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
    public string GetTermsOfServiceUri() {

      if (_directory == null)
        throw new NullReferenceException();

      return _directory.Meta.TermsOfService;
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
    public async Task<Dictionary<string, string>> NewOrder(string[] hostnames, string challengeType) {
      _challenges.Clear();

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, response) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, false, letsEncryptOrder);

      if (order.Status == "ready")
        return new Dictionary<string, string>();

      if (order.Status != "pending")
        throw new InvalidOperationException($"Created new order and expected status 'pending', but got: {order.Status} \r\n {response}");
      
      _currentOrder = order;

      var results = new Dictionary<string, string>();
      foreach (var item in order.Authorizations) {
        var (challengeResponse, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, item, true, null);

        if (challengeResponse.Status == "valid")
          continue;

        if (challengeResponse.Status != "pending")
          throw new InvalidOperationException($"Expected autorization status 'pending', but got: {order.Status} \r\n {responseText}");

        var challenge = challengeResponse.Challenges.First(x => x.Type == challengeType);
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
                results[challengeResponse.Identifier.Value] = dnsToken;
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
              results[challengeResponse.Identifier.Value] = keyToken;
              break;
            }

          default:
            throw new NotImplementedException();
        }
      }

      return results;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task CompleteChallenges() {

      for (var index = 0; index < _challenges.Count; index++) {

        var challenge = _challenges[index];

        while (true) {
          AuthorizeChallenge authorizeChallenge = new AuthorizeChallenge();

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

          var (result, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, false, "{}");

          if (result.Status == "valid")
            break;
          if (result.Status != "pending")
            throw new InvalidOperationException($"Failed autorization of {_currentOrder.Identifiers[index].Value} \r\n {responseText}");

          await Task.Delay(1000);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hostnames"></param>
    /// <returns></returns>
    public async Task GetOrder(string[] hostnames) {

      var letsEncryptOrder = new Order {
        Expires = DateTime.UtcNow.AddDays(2),
        Identifiers = hostnames.Select(hostname => new OrderIdentifier {
          Type = "dns",
          Value = hostname
        }).ToArray()
      };

      var (order, response) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, false, letsEncryptOrder);

      _currentOrder = order;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subject"></param>
    /// <returns>Cert and Private key</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<(X509Certificate2 Cert, RSA PrivateKey)> GetCertificate(string subject) {

      _logger.LogInformation($"Invoked: {nameof(GetCertificate)}");


      if (_currentOrder == null)
        throw new ArgumentNullException();

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
          var (response, responseText) = await SendAsync<Order>(HttpMethod.Post, _currentOrder.Finalize, false, letsEncryptOrder);

          if (response.Status == "processing")
            (response, responseText) = await SendAsync<Order>(HttpMethod.Post, _currentOrder.Location, true, null);

          if (response.Status == "valid") {
            certificateUrl = response.Certificate;
          }
        }

        if ((start - DateTime.UtcNow).Seconds > 120)
          throw new TimeoutException();

        await Task.Delay(1000);
        continue;

        // throw new InvalidOperationException(/*$"Invalid order status: "*/);
      }

      var (pem, _) = await SendAsync<string>(HttpMethod.Post, certificateUrl, true, null);

      if (_cache == null)
        throw new NullReferenceException();

      _cache.CachedCerts ??= new Dictionary<string, CertificateCache>();
      _cache.CachedCerts[subject] = new CertificateCache {
        Cert = pem,
        Private = key.ExportCspBlob(true)
      };

      var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pem));

      return (cert, key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task KeyChange() {
      throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RevokeCertificate() {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Main method used to send data to LetsEncrypt
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="method"></param>
    /// <param name="uri"></param>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<(TResult, string)> SendAsync<TResult>(HttpMethod method, Uri uri, bool isPostAsGet, object? message) where TResult : class {
      var request = new HttpRequestMessage(method, uri);

      _nonce = uri.OriginalString != "directory"
        ? await NewNonce()
        : default;

      if (message != null || isPostAsGet) {
        var jwsHeader = new JwsHeader {
          Url = uri,
        };

        if (_nonce != null)
          jwsHeader.Nonce = _nonce;

        var encodedMessage = isPostAsGet
          ? _jwsService.Encode(jwsHeader)
          : _jwsService.Encode(message, jwsHeader);

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

      if (response.Content.Headers.ContentType.MediaType == "application/problem+json") {
        var problemJson = await response.Content.ReadAsStringAsync();
        var problem = problemJson.ToObject<Problem>();
        problem.RawJson = problemJson;
        throw new LetsEncrytException(problem, response);
      }

      var responseText = await response.Content.ReadAsStringAsync();

      if (typeof(TResult) == typeof(string) && response.Content.Headers.ContentType.MediaType == "application/pem-certificate-chain") {
        return ((TResult)(object)responseText, null);
      }

      var responseContent = responseText.ToObject<TResult>();

      if (responseContent is IHasLocation ihl) {
        if (response.Headers.Location != null)
          ihl.Location = response.Headers.Location;
      }

      return (responseContent, responseText);
    }

    /// <summary>
    /// Request New Nonce to be able to start POST requests
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<string> NewNonce() {
      if (_directory == null)
        throw new NotImplementedException();

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, _directory.NewNonce));
      return result.Headers.GetValues("Replay-Nonce").First();
    }
  }
}