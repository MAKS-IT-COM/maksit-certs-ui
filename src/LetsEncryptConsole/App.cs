using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MaksIT.Core.Extensions;

using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncryptConsole.Services;

using MaksIT.SSHProvider;

namespace MaksIT.LetsEncryptConsole;

public interface IApp {

  Task Run(string[] args);
}

public class App : IApp {

  private readonly string _appPath = AppDomain.CurrentDomain.BaseDirectory;

  private readonly ILogger<App> _logger;
  private readonly Configuration _appSettings;
  private readonly ILetsEncryptService _letsEncryptService;
  private readonly ITerminalService _terminalService;

  public App(
    ILogger<App> logger,
    IOptions<Configuration> appSettings,
    ILetsEncryptService letsEncryptService,
    ITerminalService terminalService
  ) {
    _logger = logger;
    _appSettings = appSettings.Value;
    _letsEncryptService = letsEncryptService;
    _terminalService = terminalService;
  }

  public async Task Run(string[] args) {

    _logger.LogInformation("Letsencrypt client estarted...");

    foreach (var env in _appSettings.Environments?.Where(x => x.Active) ?? new List<LetsEncryptEnvironment>()) {
      try {
        _logger.LogInformation($"Let's Encrypt C# .Net Core Client, environment: {env.Name}");

        //loop all customers
        foreach (Customer customer in _appSettings.Customers?.Where(x => x.Active) ?? new List<Customer>()) {
          try {
            _logger.LogInformation($"Managing customer: {customer.Id} - {customer.Name} {customer.LastName}");

            //define cache folder
            string cachePath = Path.Combine(_appPath, customer.Id, env.Name, "cache");
            if (!Directory.Exists(cachePath)) {
              Directory.CreateDirectory(cachePath);
            }

            //check acme directory
            var acmePath = Path.Combine(_appPath, customer.Id, env.Name, "acme");
            if (!Directory.Exists(acmePath)) {
              Directory.CreateDirectory(acmePath);
            }

            //loop each customer website
            foreach (Site site in customer.Sites?.Where(s => s.Active) ?? new List<Site>()) {
              _logger.LogInformation($"Managing site: {site.Name}");

              try {
                //create folder for ssl
                string sslPath = Path.Combine(_appPath, customer.Id, env.Name, "ssl", site.Name);
                if (!Directory.Exists(sslPath)) {
                  Directory.CreateDirectory(sslPath);
                }

                var cacheFile = Path.Combine(cachePath, $"{site.Name}.lets-encrypt.cache.json");

                //1. Client initialization
                _logger.LogInformation("1. Client Initialization...");

                #region LetsEncrypt client configuration
                await _letsEncryptService.ConfigureClient(env.Url);
                #endregion

                #region LetsEncrypt local registration cache initialization
                var registrationCache = (File.Exists(cacheFile)
                  ? File.ReadAllText(cacheFile)
                  : null)
                  .ToObject<RegistrationCache>();

                await _letsEncryptService.Init(customer.Contacts, registrationCache);
                #endregion

                #region LetsEncrypt terms of service
                _logger.LogInformation($"Terms of service: {_letsEncryptService.GetTermsOfServiceUri()}");
                #endregion

                // get cached certificate and check if it's valid
                // if valid check if cert and key exists otherwise recreate
                // else continue with new certificate request
                var certRes = new CachedCertificateResult();
                if (registrationCache != null && registrationCache.TryGetCachedCertificate(site.Name, out certRes)) {

                  File.WriteAllText(Path.Combine(sslPath, $"{site.Name}.crt"), certRes.Certificate);
                  
                  if (certRes.PrivateKey != null)
                    File.WriteAllText(Path.Combine(sslPath, $"{site.Name}.key"), certRes.PrivateKey.ExportRSAPrivateKeyPem());

                  _logger.LogInformation("Certificate and Key exists and valid. Restored from cache.");
                }
                else {

                  //try to make new order
                  try {
                    //create new orders
                    Console.WriteLine("2. Client New Order...");

                    #region LetsEncrypt new order
                    var orders = await _letsEncryptService.NewOrder(site.Hosts, site.Challenge);
                    #endregion

                    if (orders.Count > 0) {
                      switch (site.Challenge) {
                        case "http-01": {
                            //ensure to enable static file discovery on server in .well-known/acme-challenge
                            //and listen on 80 port

                            foreach (FileInfo file in new DirectoryInfo(acmePath).GetFiles())
                              file.Delete();

                            foreach (var result in orders) {
                              Console.WriteLine($"Key: {result.Key}, Value: {result.Value}");
                              string[] splitToken = result.Value.Split('.');

                              File.WriteAllText(Path.Combine(acmePath, splitToken[0]), result.Value);
                            }

                            foreach (FileInfo file in new DirectoryInfo(acmePath).GetFiles()) {
                              if (env?.SSH?.Active ?? false) {
                                UploadFiles(_logger, env.SSH, env.ACME.Linux.Path, file.Name, File.ReadAllBytes(file.FullName), env.ACME.Linux.Owner, env.ACME.Linux.ChangeMode);
                              }
                              else {
                                throw new NotImplementedException();
                              }
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
                      _logger.LogInformation("3. Client Complete Challange...");
                      await _letsEncryptService.CompleteChallenges();
                      _logger.LogInformation("Challanges comleted.");
                      #endregion

                      await Task.Delay(1000);

                      #region Download new certificate
                      _logger.LogInformation("4. Download certificate...");
                      var (cert, key) = await _letsEncryptService.GetCertificate(site.Name);
                      #endregion

                      #region Persist cache
                      registrationCache = _letsEncryptService.GetRegistrationCache();
                      File.WriteAllText(cacheFile, registrationCache.ToJson());
                      #endregion
                    }

                    #region Save cert and key to filesystem
                    certRes = new CachedCertificateResult();
                    if (registrationCache.TryGetCachedCertificate(site.Name, out certRes)) {

                      File.WriteAllText(Path.Combine(sslPath, $"{site.Name}.crt"), certRes.Certificate);

                      if(certRes.PrivateKey != null)
                        File.WriteAllText(Path.Combine(sslPath, $"{site.Name}.key"), certRes.PrivateKey.ExportRSAPrivateKeyPem());

                      _logger.LogInformation("Certificate saved.");

                      foreach (FileInfo file in new DirectoryInfo(sslPath).GetFiles()) {

                        if (env?.SSH?.Active ?? false) {
                          UploadFiles(_logger, env.SSH, $"{env.SSL.Linux.Path}/{site.Name}", file.Name, File.ReadAllBytes(file.FullName), env.SSL.Linux.Owner, env.SSL.Linux.ChangeMode);
                        }
                        else {
                          throw new NotImplementedException();
                        }
                      }

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

    


  private void UploadFiles(
    ILogger logger,
    SSHClientSettings sshSettings,
    string workDir,
    string fileName,
    byte [] bytes,
    string owner,
    string changeMode
  ) {
    using var sshService = new SSHService(logger, sshSettings.Host, sshSettings.Port, sshSettings.Username, sshSettings.Password);
    sshService.Connect();

    sshService.RunSudoCommand(sshSettings.Password, $"mkdir {workDir}");

    sshService.RunSudoCommand(sshSettings.Password, $"chown {owner} {workDir} -R");
    sshService.RunSudoCommand(sshSettings.Password, $"chmod 777 {workDir} -R");

    sshService.Upload($"{workDir}", fileName, bytes);

    sshService.RunSudoCommand(sshSettings.Password, $"chown {owner} {workDir} -R");
    sshService.RunSudoCommand(sshSettings.Password, $"chmod {changeMode} {workDir} -R");
  }
}
