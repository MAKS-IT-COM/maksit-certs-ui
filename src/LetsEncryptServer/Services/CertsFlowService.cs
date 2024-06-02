using System.Text;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

using DomainResults.Common;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncryptServer.Models.Requests;
using MaksIT.SSHProvider;


namespace MaksIT.LetsEncryptServer.Services;

public interface ICertsFlowServiceBase {
  (string?, IDomainResult) AcmeChallenge(string fileName);
}

public interface ICertsFlowService : ICertsFlowServiceBase {
  Task<(Guid?, IDomainResult)> ConfigureClientAsync();
  (string?, IDomainResult) GetTermsOfService(Guid sessionId);
  Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData);
  Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, NewOrderRequest requestData);
  Task<IDomainResult> CompleteChallengesAsync(Guid sessionId);
  Task<IDomainResult> GetOrderAsync(Guid sessionId, GetOrderRequest requestData);
  Task<IDomainResult> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData);
  (Dictionary<string, string>?, IDomainResult) ApplyCertificates(Guid sessionId, GetCertificatesRequest requestData);
}

public class CertsFlowService : ICertsFlowService {

  private readonly Configuration _appSettings;
  private readonly ILogger<CertsFlowService> _logger;
  private readonly ILetsEncryptService _letsEncryptService;
  private readonly ICacheService _cacheService;

  private readonly string _acmePath;

  public CertsFlowService(
    IOptions<Configuration> appSettings,
    ILogger<CertsFlowService> logger,
    ILetsEncryptService letsEncryptService,
    ICacheService cashService
  ) {
    _appSettings = appSettings.Value;
    _logger = logger;
    _letsEncryptService = letsEncryptService;
    _cacheService = cashService;

    _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");
    if (!Directory.Exists(_acmePath))
      Directory.CreateDirectory(_acmePath);
  }

  public async Task<(Guid?, IDomainResult)> ConfigureClientAsync() {
    var sessionId = Guid.NewGuid();

    var url = _appSettings.DevMode
      ? _appSettings.Staging
      : _appSettings.Production;

    var result = await _letsEncryptService.ConfigureClient(sessionId, url);
    if (!result.IsSuccess)
      return (null, result);

    return IDomainResult.Success(sessionId);
  }

  public (string?, IDomainResult) GetTermsOfService(Guid sessionId) {
    var (terms, getTermsResult) = _letsEncryptService.GetTermsOfServiceUri(sessionId);
    if (!getTermsResult.IsSuccess || terms == null)
      return (null, getTermsResult);

    return IDomainResult.Success<string>(terms);
  }

  public async Task<(Guid?, IDomainResult)> InitAsync(Guid sessionId, Guid? accountId, InitRequest requestData) {
    RegistrationCache? cache = null;

    if (accountId == null) {
      accountId = Guid.NewGuid();
    }
    else {
      var (loadedCache, loadCaceResutl) = await _cacheService.LoadFromCacheAsync(accountId.Value);
      if (!loadCaceResutl.IsSuccess || loadCaceResutl == null) {
        accountId = Guid.NewGuid();
      }
      else {
        cache = loadedCache;
      }
    }

    var result = await _letsEncryptService.Init(sessionId, accountId.Value, requestData.Contacts, cache);
    return result.IsSuccess ? IDomainResult.Success<Guid>(accountId.Value) : (null, result);
  }

  public async Task<(List<string>?, IDomainResult)> NewOrderAsync(Guid sessionId, NewOrderRequest requestData) {
    var (results, newOrderResult) = await _letsEncryptService.NewOrder(sessionId, requestData.Hostnames, requestData.ChallengeType);
  if (!newOrderResult.IsSuccess || results == null)
      return (null, newOrderResult);

    var challenges = new List<string>();
    foreach (var result in results) {
      string[] splitToken = result.Value.Split('.');
      File.WriteAllText(Path.Combine(_acmePath, splitToken[0]), result.Value);
      challenges.Add(splitToken[0]);
    }

    return IDomainResult.Success(challenges);
  }

  public async Task<IDomainResult> CompleteChallengesAsync(Guid sessionId) {
    return await _letsEncryptService.CompleteChallenges(sessionId);
  }

  public async Task<IDomainResult> GetOrderAsync(Guid sessionId, GetOrderRequest requestData) {
    return await _letsEncryptService.GetOrder(sessionId, requestData.Hostnames);
  }

  public async Task<IDomainResult> GetCertificatesAsync(Guid sessionId, GetCertificatesRequest requestData) {
    foreach (var subject in requestData.Hostnames) {
      var result = await _letsEncryptService.GetCertificate(sessionId, subject);
      if (!result.IsSuccess)
        return result;

      Thread.Sleep(1000);
    }

    // Persist the cache
    var (cache, getCacheResult) = _letsEncryptService.GetRegistrationCache(sessionId);
    if (!getCacheResult.IsSuccess || cache == null)
      return getCacheResult;

    var saveResult = await _cacheService.SaveToCacheAsync(cache.AccountId, cache);
    if(!saveResult.IsSuccess)
      return saveResult;

    return IDomainResult.Success();
  }

  public (Dictionary<string, string>?, IDomainResult) ApplyCertificates(Guid sessionId, GetCertificatesRequest requestData) {
    var results = new Dictionary<string, string>();

    foreach (var subject in requestData.Hostnames) {
      var (cert, getCertResult) = _letsEncryptService.TryGetCachedCertificate(sessionId, subject);
      if (!getCertResult.IsSuccess || cert == null)
        return (null, getCertResult);

      var content = $"{cert.Certificate}\n{cert.PrivateKeyPem}";
      results.Add(subject, content);
    }

    var uploadResult = UploadToServer(results);
    if (!uploadResult.IsSuccess)
      return (null, uploadResult);

    //var notifyResult = NotifyHaproxy(results);
    //if (!notifyResult.IsSuccess)
    //  return (null, notifyResult);

    var reloadResult = ReloadServer();
    if (!reloadResult.IsSuccess)
      return (null, reloadResult);

    return IDomainResult.Success(results);
  }

  public (string?, IDomainResult) AcmeChallenge(string fileName) {
    DeleteExporedChallenges();

    var fileContent = File.ReadAllText(Path.Combine(_acmePath, fileName));
    if (fileContent == null)
      return IDomainResult.NotFound<string?>();

    return IDomainResult.Success(fileContent);
  }

  private IDomainResult UploadToServer(Dictionary<string, string> results) {
    var server = _appSettings.Server;

    try {
      using (SSHService sshClient = (server.PrivateKeys != null && server.PrivateKeys.Any(x => !string.IsNullOrWhiteSpace(x)))
        ? new SSHService(_logger, server.Ip, server.SSHPort, server.Username, server.PrivateKeys)
        : !string.IsNullOrWhiteSpace(server.Password)
          ? new SSHService(_logger, server.Ip, server.SSHPort, server.Username, server.Password)
          : throw new ArgumentNullException("Neither private keys nor password was provided")) {

        var sshConnectResult = sshClient.Connect();
        if (!sshConnectResult.IsSuccess)
          return sshConnectResult;

        foreach (var result in results) {
          var uploadResult = sshClient.Upload(server.Path, result.Key, Encoding.UTF8.GetBytes(result.Value));
          if (!uploadResult.IsSuccess)
            return uploadResult;
        }
      }
    }
    catch (Exception ex) {
      var message = "Unable to upload files to remote server";
      _logger.LogError(ex, message);

      return IDomainResult.CriticalDependencyError(message);
    }

    return IDomainResult.Success();
  }
  private IDomainResult ReloadServer() {
    var server = _appSettings.Server;

    try {
      using (SSHService sshClient = (server.PrivateKeys != null && server.PrivateKeys.Any(x => !string.IsNullOrWhiteSpace(x)))
        ? new SSHService(_logger, server.Ip, server.SSHPort, server.Username, server.PrivateKeys)
        : !string.IsNullOrWhiteSpace(server.Password)
          ? new SSHService(_logger, server.Ip, server.SSHPort, server.Username, server.Password)
          : throw new ArgumentNullException("Neither private keys nor password was provided")) {

        var sshConnectResult = sshClient.Connect();
        if (!sshConnectResult.IsSuccess)
          return sshConnectResult;

        // TODO: Prefer to create the native linux service which can receive the signal to reload the services
        return sshClient.RunSudoCommand("", "systemctl reload haproxy");
      }
    }
    catch (Exception ex) {
      var message = "Unable to upload files to remote server";
      _logger.LogError(ex, message);

      return IDomainResult.CriticalDependencyError(message);
    }

    return IDomainResult.Success();
  }

  /// <summary>
  /// Currently not working
  /// </summary>
  /// <param name="results"></param>
  /// <returns></returns>
  private IDomainResult NotifyHaproxy(Dictionary<string, string> results) {
    var server = _appSettings.Server;

    try {
      using (var client = new TcpClient(server.Ip, server.SocketPort))
      using (var networkStream = client.GetStream())
      using (var writer = new StreamWriter(networkStream, Encoding.ASCII))
      using (var reader = new StreamReader(networkStream, Encoding.ASCII)) {
        writer.AutoFlush = true;

        foreach (var result in results) {
          var certFile = result.Key;

          // Prepare the certificate
          string prepareCommand = $"new ssl cert {server.Path}/{certFile}";
          writer.WriteLine(prepareCommand);
          writer.Flush();
          string prepareResponse = reader.ReadLine();
          //if (prepareResponse.Contains("error", StringComparison.OrdinalIgnoreCase)) {
          //  _logger.LogError($"Error while preparing certificate {certFile}: {prepareResponse}");
          //  return IDomainResult.CriticalDependencyError($"Error while preparing certificate {certFile}");
          //}

          // Set the certificate
          string setCommand = $"set ssl cert {server.Path}/{certFile} <<\n{result.Value}\n";
          writer.WriteLine(setCommand);
          writer.Flush();
          string setResponse = reader.ReadLine();
          //if (setResponse.Contains("error", StringComparison.OrdinalIgnoreCase)) {
          //  _logger.LogError($"Error while setting certificate {certFile}: {setResponse}");
          //  return IDomainResult.CriticalDependencyError($"Error while setting certificate {certFile}");
          //}

          // Commit the certificate
          string commitCommand = $"commit ssl cert {server.Path}/{certFile}";
          writer.WriteLine(commitCommand);
          writer.Flush();
          string commitResponse = reader.ReadLine();
          //if (commitResponse.Contains("error", StringComparison.OrdinalIgnoreCase)) {
          //  _logger.LogError($"Error while committing certificate {certFile}: {commitResponse}");
          //  return IDomainResult.CriticalDependencyError($"Error while committing certificate {certFile}");
          //}
        }

        _logger.LogInformation("Certificates committed successfully.");
      }
    }
    catch (Exception ex) {
      var message = "An error occurred while committing certificates";
      _logger.LogError(ex, message);

      return IDomainResult.CriticalDependencyError(message);
    }

    return IDomainResult.Success();
  }




  private void DeleteExporedChallenges() {
    var currentDate = DateTime.Now;

    foreach (var file in Directory.GetFiles(_acmePath)) {
      try {
        var creationTime = File.GetCreationTime(file);

        // Calculate the time difference
        var timeDifference = currentDate - creationTime;

        // If the file is older than 1 day, delete it
        if (timeDifference.TotalDays > 1) {


          File.Delete(file);
          _logger.LogInformation($"Deleted file: {file}");
        }
      }
      catch (Exception ex) {
        _logger.LogWarning(ex, "File cannot be deleted");
      }
    }
  }
}
