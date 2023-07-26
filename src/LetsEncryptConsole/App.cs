using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncryptConsole.Services;

using MaksIT.Core.Extensions;
using System.Text.Json;

namespace MaksIT.LetsEncryptConsole {

  public interface IApp {

    Task Run(string[] args);
  }

  public class App : IApp {

    private readonly string _appPath = AppDomain.CurrentDomain.BaseDirectory;

    private readonly ILogger<App> _logger;
    private readonly Configuration _appSettings;
    private readonly ILetsEncryptService _letsEncryptService;
    private readonly IJwsService _jwsService;
    private readonly IKeyService _keyService;
    private readonly ITerminalService _terminalService;

    public App(
      ILogger<App> logger,
      IOptions<Configuration> appSettings,
      ILetsEncryptService letsEncryptService,
      IJwsService jwsService,
      IKeyService keyService,
      ITerminalService terminalService
    ) {
      _logger = logger;
      _appSettings = appSettings.Value;
      _letsEncryptService = letsEncryptService;
      _jwsService = jwsService;
      _keyService = keyService;
      _terminalService = terminalService;
    }

    public async Task Run(string[] args) {

      foreach (var env in _appSettings.Environments?.Where(x => x.Active) ?? new List<LetsEncryptEnvironment>()) {
        try {
          _logger.LogTrace($"Let's Encrypt C# .Net Core Client, environment: {env.Name}");

          //loop all customers
          foreach (Customer customer in _appSettings.Customers?.Where(x => x.Active) ?? new List<Customer>()) {
            try {
              _logger.LogTrace($"Managing customer: {customer.Id} - {customer.Name} {customer.LastName}");

              //loop each customer website
              foreach (Site site in customer.Sites?.Where(s => s.Active) ?? new List<Site>()) {
                _logger.LogTrace($"Managing site: {site.Name}");

                try {
                  //define cache folder
                  string cacheFolder = Path.Combine(_appPath, env.Cache, customer.Id);
                  if (!Directory.Exists(cacheFolder)) {
                    Directory.CreateDirectory(cacheFolder);
                  }

                  //1. Client initialization
                  _logger.LogTrace("1. Client Initialization...");

                  #region LetsEncrypt client configuration
                  await _letsEncryptService.ConfigureClient(env.Url, customer.Contacts);
                  #endregion

                  #region LetsEncrypt local registration cache initialization
                  var hash = SHA256.HashData(Encoding.UTF8.GetBytes(site.Name));
                  var cacheFileName = _jwsService.Base64UrlEncoded(hash) + ".lets-encrypt.cache.json";
                  var cachePath = Path.Combine(cacheFolder, cacheFileName);

                  var cacheFile = File.Exists(cachePath)
                    ? File.ReadAllText(cachePath)
                    : null;

                  var registrationCache = cacheFile.ToObject<RegistrationCache>();
                  await _letsEncryptService.Init(registrationCache);
                  registrationCache = _letsEncryptService.GetRegistrationCache();
                  #endregion

                  #region LetsEncrypt terms of service
                  _logger.LogTrace($"Terms of service: {_letsEncryptService.GetTermsOfServiceUri()}");
                  #endregion

                  //create folder for ssl
                  string ssl = Path.Combine(env.GetSSL(), site.Name);
                  if (!Directory.Exists(ssl)) {
                    Directory.CreateDirectory(ssl);
                  }

                  // get cached certificate and check if it's valid
                  // if valid check if cert and key exists otherwise recreate
                  // else continue with new certificate request
                  var certRes = new CachedCertificateResult();
                  if (TryGetCachedCertificate(registrationCache, site.Name, out certRes)) {
                    string cert = Path.Combine(ssl, $"{site.Name}.crt");
                    //if(!File.Exists(cert))
                    File.WriteAllText(cert, certRes.Certificate);

                    string key = Path.Combine(ssl, $"{site.Name}.key");
                    //if(!File.Exists(key)) {
                    using (StreamWriter writer = File.CreateText(key))
                      _keyService.ExportPrivateKey(certRes.PrivateKey, writer);
                    //}

                    _logger.LogTrace("Certificate and Key exists and valid. Restored from cache.");
                  }
                  else {

                    //try to make new order
                    try {
                      //create new orders
                      Console.WriteLine("2. Client New Order...");

                      #region LetsEncrypt new order
                      var orders = await _letsEncryptService.NewOrder(site.Hosts, site.Challenge);
                      #endregion

                      switch (site.Challenge) {
                        case "http-01": {
                            //ensure to enable static file discovery on server in .well-known/acme-challenge
                            //and listen on 80 port

                            //check acme directory
                            var acme = env.GetACME();

                            if (!Directory.Exists(acme)) {
                              Directory.CreateDirectory(acme);
                            }

                            foreach (FileInfo file in new DirectoryInfo(acme).GetFiles()) {
                              if (file.LastWriteTimeUtc < DateTime.UtcNow.AddMonths(-3))
                                file.Delete();
                            }


                            foreach (var result in orders) {
                              Console.WriteLine($"Key: {result.Key}, Value: {result.Value}");
                              string[] splitToken = result.Value.Split('.');

                              File.WriteAllText(Path.Combine(env.GetACME(), splitToken[0]), result.Value);
                            }

                            if (OperatingSystem.IsLinux()) {
                              _terminalService.Exec($"chgrp -R nginx {env.GetACME()}");
                              _terminalService.Exec($"chmod -R g+rwx {env.GetACME()}");
                            }

                            break;
                          }

                        case "dns-01": {
                            //Manage DNS server MX record, depends from provider
                            throw new NotImplementedException();
                          }

                        default: {
                            throw new NotImplementedException();
                          }
                      }

                      #region LetsEncrypt complete challenges
                      _logger.LogTrace("3. Client Complete Challange...");
                      await _letsEncryptService.CompleteChallenges();
                      _logger.LogTrace("Challanges comleted.");
                      #endregion

                      await Task.Delay(1000);

                      // Download new certificate
                      _logger.LogTrace("4. Download certificate...");
                      var (cert, key) = await _letsEncryptService.GetCertificate(site.Name);

                      #region Persist cache
                      registrationCache = _letsEncryptService.GetRegistrationCache();
                      File.WriteAllText(cachePath, registrationCache.ToJson());
                      #endregion

                      #region Save cert and key to filesystem
                      certRes = new CachedCertificateResult();
                      if (TryGetCachedCertificate(registrationCache, site.Name, out certRes)) {
                        string certPath = Path.Combine(ssl, site.Name + ".crt");
                        File.WriteAllText(certPath, certRes.Certificate);

                        string keyPath = Path.Combine(ssl, site.Name + ".key");
                        using (var writer = File.CreateText(keyPath))
                          _keyService.ExportPrivateKey(certRes.PrivateKey, writer);

                        _logger.LogTrace("Certificate saved.");
                      }
                      else {
                        _logger.LogError("Unable to get new cached certificate.");
                      }
                      #endregion
                    }
                    catch (Exception ex) {
                      _logger.LogError(ex, "");
                      await _letsEncryptService.GetOrder(site.Hosts);
                    }

                  }


                  

                }
                catch (Exception ex) {
                  _logger.LogError(ex, "Customer unhandled error");
                }
              }
            }
            catch (Exception ex) {
              _logger.LogError(ex, "Environment unhandled error");
            }
          }

          if (env.Name == "ProductionV2") {
            _terminalService.Exec("systemctl restart nginx");
          }
        }
        catch (Exception ex) {
          _logger.LogError(ex.Message.ToString());
          break;
        }
      }
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool TryGetCachedCertificate(RegistrationCache? registrationCache, string subject, out CachedCertificateResult? value) {
      value = null;

      if (registrationCache?.CachedCerts == null)
        return false;

      if (!registrationCache.CachedCerts.TryGetValue(subject, out var cache)) {
        return false;
      }
  
      var cert = new X509Certificate2(Encoding.ASCII.GetBytes(cache.Cert));

      // if it is about to expire, we need to refresh
      if ((cert.NotAfter - DateTime.UtcNow).TotalDays < 30)
        return false;

      var rsa = new RSACryptoServiceProvider(4096);
      rsa.ImportCspBlob(cache.Private);

      value = new CachedCertificateResult {
        Certificate = cache.Cert,
        PrivateKey = rsa
      };
      return true;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="hostsToRemove"></param>
    public RegistrationCache? ResetCachedCertificate(RegistrationCache? registrationCache, IEnumerable<string> hostsToRemove) {
      if (registrationCache != null)
        foreach (var host in hostsToRemove)
          registrationCache.CachedCerts.Remove(host);

      return registrationCache;
    }
  }
}
