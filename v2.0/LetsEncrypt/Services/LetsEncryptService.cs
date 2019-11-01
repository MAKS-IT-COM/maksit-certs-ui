using System;

using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using System.Net.Http;

using System.IO;
using System.Text;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using LetsEncrypt.Entities;
using LetsEncrypt.Exceptions;



namespace LetsEncrypt.Services {

    public interface ILetsEncryptService {
        Task Init(string url, string home, string siteName, string[] contacts, CancellationToken token = default(CancellationToken));
        string GetTermsOfServiceUri(CancellationToken token = default(CancellationToken));
        bool TryGetCachedCertificate(string subject, out CachedCertificateResult value);
        Task NewNonce(CancellationToken token = default(CancellationToken));
        Task<Dictionary<string, string>> NewOrder(string[] hostnames, string challengeType, CancellationToken token = default(CancellationToken));
        Task CompleteChallenges(CancellationToken token = default(CancellationToken));
        Task GetOrder(string[] hostnames, CancellationToken token = default(CancellationToken));
        Task<(X509Certificate2 Cert, RSA PrivateKey)> GetCertificate(string subject, CancellationToken token = default(CancellationToken));
    }




    public class LetsEncryptService: ILetsEncryptService {

        private readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private readonly IJwsService _jwsService;



        private string _path;
        private string _url;
        private string _home;
        private string _nonce;

        private RSACryptoServiceProvider _accountKey;

        private RegistrationCache _cache;
        private HttpClient _client;
        private AcmeDirectory _directory;
        private List<AuthorizationChallenge> _challenges = new List<AuthorizationChallenge>();
        private Order _currentOrder;
        
        
        public LetsEncryptService(IJwsService jwsService) {
            _jwsService = jwsService;
        }

        /// <summary>
        /// Account creation or Initialization from cache
        /// </summary>
        /// <param name="contacts"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Init(string url, string home, string siteName, string[] contacts, CancellationToken token = default(CancellationToken)) {
            // old Letsencrypt constructor
            _url = url ?? throw new ArgumentNullException(nameof(url));
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(siteName));

            _home = home ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

            var file = _jwsService.Base64UrlEncoded(hash) + ".lets-encrypt.cache.json";
            _path = Path.Combine(_home, file);


            // originally Init part was here
            _accountKey = new RSACryptoServiceProvider(4096);
            _client = GetCachedClient(_url);

            // 1 - Get directory
            (_directory, _) = await SendAsync<AcmeDirectory>(HttpMethod.Get, new Uri("directory", UriKind.Relative), null, token);


            if (File.Exists(_path))
            {
                bool success;
                try
                {
                    lock (Locker)
                    {
                        _cache = JsonConvert.DeserializeObject<RegistrationCache>(File.ReadAllText(_path));
                    }

                    _accountKey.ImportCspBlob(_cache.AccountKey);
                    //_jws = new Jws(_accountKey, _cache.Id);
                    success = true;
                }
                catch
                {
                    success = false;
                    // if we failed for any reason, we'll just
                    // generate a new registration
                }

                if (success)
                {
                    return;
                }
            }

            await NewNonce();

            //New Account request
            _jwsService.Init(_accountKey, null);
            var (account, response) = await SendAsync<Account>(HttpMethod.Post, _directory.NewAccount, new Account
            {
                // we validate this in the UI before we get here, so that is fine
                TermsOfServiceAgreed = true,
                Contacts = contacts.Select(contact =>
                    string.Format("mailto:{0}", contact)
                ).ToArray()

            }, token);
            _jwsService.SetKeyId(account);

            if (account.Status != "valid")
                throw new InvalidOperationException("Account status is not valid, was: " + account.Status + Environment.NewLine + response);

            lock (Locker)
            {
                _cache = new RegistrationCache
                {
                    Location = account.Location,
                    AccountKey = _accountKey.ExportCspBlob(true),
                    Id = account.Id,
                    Key = account.Key
                };
                File.WriteAllText(_path, JsonConvert.SerializeObject(_cache, Formatting.Indented));
            }
        }


        /// <summary>
        /// Just retrive terms of service
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string GetTermsOfServiceUri(CancellationToken token = default(CancellationToken))
        {
            return _directory.Meta.TermsOfService;
        }

        /// <summary>
        /// Request New Nonce to be able to start POST requests
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task NewNonce(CancellationToken token = default(CancellationToken))
        {
            var result = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, _directory.NewNonce)).ConfigureAwait(false);
            _nonce = result.Headers.GetValues("Replay-Nonce").First();
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
        public async Task<Dictionary<string, string>> NewOrder(string[] hostnames, string challengeType, CancellationToken token = default(CancellationToken)) {
            _challenges.Clear();

            //update jws with account url
            _jwsService.Init(_accountKey, _cache.Location.ToString());

            var (order, response) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, new Order
            {
                Expires = DateTime.UtcNow.AddDays(2),
                Identifiers = hostnames.Select(hostname => new OrderIdentifier
                {
                    Type = "dns",
                    Value = hostname
                }).ToArray()
            }, token);

            if (order.Status != "pending")
                throw new InvalidOperationException("Created new order and expected status 'pending', but got: " + order.Status + Environment.NewLine + 
                    response);
            _currentOrder = order;

            var results = new Dictionary<string, string>();
            foreach (var item in order.Authorizations)
            {
                var (challengeResponse, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Get, item, null, token);
                if (challengeResponse.Status == "valid")
                    continue;

                if (challengeResponse.Status != "pending")
                    throw new InvalidOperationException("Expected autorization status 'pending', but got: " + order.Status + 
                        Environment.NewLine + responseText);

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
                            using (var sha256 = SHA256.Create())
                            {
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
                            results[challengeResponse.Identifier.Value] = challenge.Token + "~" + keyToken;
                            break;
                    }

                }



            }

            return results;
        }




        public async Task CompleteChallenges(CancellationToken token = default(CancellationToken))
        {
            _jwsService.Init(_accountKey, _cache.Location.ToString());

            for (var index = 0; index < _challenges.Count; index++)
            {
                var challenge = _challenges[index];

                while (true)
                {
                    AuthorizeChallenge authorizeChallenge = new AuthorizeChallenge();

                    switch (challenge.Type) {
                        case "dns-01": {
                                authorizeChallenge.KeyAuthorization = _jwsService.GetKeyAuthorization(challenge.Token);
                                break;
                            }

                        case "http-01": {
                                break;
                            }
                    }

                    var (result, responseText) = await SendAsync<AuthorizationChallengeResponse>(HttpMethod.Post, challenge.Url, authorizeChallenge, token);

                    if (result.Status == "valid")
                        break;
                    if (result.Status != "pending")
                        throw new InvalidOperationException("Failed autorization of " + _currentOrder.Identifiers[index].Value + Environment.NewLine + responseText);


                    await Task.Delay(500);
                }
            }
        }




        public async Task GetOrder(string[] hostnames, CancellationToken token = default(CancellationToken))
        {
            //update jws
            _jwsService.Init(_accountKey, _cache.Location.ToString());

            var (order, response) = await SendAsync<Order>(HttpMethod.Post, _directory.NewOrder, new Order
            {
                Expires = DateTime.UtcNow.AddDays(2),
                Identifiers = hostnames.Select(hostname => new OrderIdentifier
                {
                    Type = "dns",
                    Value = hostname
                }).ToArray()
            }, token);

            _currentOrder = order;
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<(X509Certificate2 Cert, RSA PrivateKey)> GetCertificate(string subject, CancellationToken token = default(CancellationToken))
        {
            var key = new RSACryptoServiceProvider(4096);
            var csr = new CertificateRequest("CN=" + subject,
                key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var san = new SubjectAlternativeNameBuilder();
            foreach (var host in _currentOrder.Identifiers)
                san.AddDnsName(host.Value);

            csr.CertificateExtensions.Add(san.Build());

            var (response, responseText) = await SendAsync<Order>(HttpMethod.Post, _currentOrder.Finalize, new FinalizeRequest
            {
                CSR = _jwsService.Base64UrlEncoded(csr.CreateSigningRequest())
            }, token);

            while (response.Status != "valid")
            {
                (response, responseText) = await SendAsync<Order>(HttpMethod.Get, response.Location, null, token);

                if(response.Status == "processing")
                {
                    await Task.Delay(500);
                    continue;
                }
                throw new InvalidOperationException("Invalid order status: " + response.Status + Environment.NewLine +
                    responseText);
            }
            var (pem, _) = await SendAsync<string>(HttpMethod.Get, response.Certificate, null, token);

            var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pem));

            _cache.CachedCerts[subject] = new CertificateCache
            {
                Cert = pem,
                Private = key.ExportCspBlob(true)
            };

            lock (Locker)
            {
                File.WriteAllText(_path,
                    JsonConvert.SerializeObject(_cache, Formatting.Indented));
            }

            return (cert, key);
        }






        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task KeyChange(CancellationToken token = default(CancellationToken))
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task RevokeCertificate(CancellationToken token = default(CancellationToken))
        {

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
        private async Task<(TResult Result, string Response)> SendAsync<TResult>(HttpMethod method, Uri uri, object message, CancellationToken token) where TResult : class
        {
            var request = new HttpRequestMessage(method, uri);

            if (message != null)
            {
                JwsMessage encodedMessage = _jwsService.Encode(message, new JwsHeader
                {
                    Nonce = _nonce,
                    Url = uri,
                });

                var json = JsonConvert.SerializeObject(encodedMessage, jsonSettings);

                request.Content = new StringContent(json);

                var requestType = "application/json";
                if (method == HttpMethod.Post)
                    requestType = "application/jose+json";

                request.Content.Headers.Remove("Content-Type");
                request.Content.Headers.Add("Content-Type", requestType);
            }

            var response = await _client.SendAsync(request, token).ConfigureAwait(false);

            if (method == HttpMethod.Post)
                _nonce = response.Headers.GetValues("Replay-Nonce").First();
            
            if (response.Content.Headers.ContentType.MediaType == "application/problem+json")
            {
                var problemJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var problem = JsonConvert.DeserializeObject<Problem>(problemJson);
                problem.RawJson = problemJson;
                throw new LetsEncrytException(problem, response);
            }

            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (typeof(TResult) == typeof(string) && response.Content.Headers.ContentType.MediaType == "application/pem-certificate-chain")
            {
                return ((TResult)(object)responseText, null);
            }

            var responseContent = JObject.Parse(responseText).ToObject<TResult>();

            if (responseContent is IHasLocation ihl)
            {
                if (response.Headers.Location != null)
                    ihl.Location = response.Headers.Location;
            }

            return (responseContent, responseText);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hosts"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetCachedCertificate(string subject, out CachedCertificateResult value)
        {
            value = null;
            if (_cache.CachedCerts.TryGetValue(subject, out var cache) == false)
            {
                return false;
            }

            var cert = new X509Certificate2(Encoding.ASCII.GetBytes(cache.Cert));

            // if it is about to expire, we need to refresh
            if ((cert.NotAfter - DateTime.UtcNow).TotalDays < 30)
                return false;

            var rsa = new RSACryptoServiceProvider(4096);
            rsa.ImportCspBlob(cache.Private);


            value = new CachedCertificateResult
            {
                Certificate = cache.Cert,
                PrivateKey = rsa
            };
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostsToRemove"></param>
        public void ResetCachedCertificate(IEnumerable<string> hostsToRemove)
        {
            foreach (var host in hostsToRemove)
            {
                _cache.CachedCerts.Remove(host);
            }
        }



        private Dictionary<string, HttpClient> _cachedClients = new Dictionary<string, HttpClient>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     In our scenario, we assume a single single wizard progressing
        ///     and the locking is basic to the wizard progress. Adding explicit
        ///     locking to be sure that we are not corrupting disk state if user
        ///     is explicitly calling stuff concurrently (running the setup wizard
        ///     from two tabs?)
        /// </summary>
        private readonly object Locker = new object();
        private HttpClient GetCachedClient(string url) {

            if (_cachedClients.TryGetValue(url, out var value)) {
                return value;
            }

            lock (Locker) {
                if (_cachedClients.TryGetValue(url, out value)) {
                    return value;
                }

                value = new HttpClient {
                    BaseAddress = new Uri(url)
                };

                _cachedClients = new Dictionary<string, HttpClient>(_cachedClients, StringComparer.OrdinalIgnoreCase) {
                    [url] = value
                };
                return value;
            }
        }

    }


}