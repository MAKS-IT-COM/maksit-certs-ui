using System.Text;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

using DomainResults.Common;

using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Services;
using MaksIT.LetsEncryptServer.Models.Requests;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;


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
  private readonly string _acmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acme");

  public CertsFlowService(
    IOptions<Configuration> appSettings,
    ILogger<CertsFlowService> logger,
    ILetsEncryptService letsEncryptService
  ) {
    _appSettings = appSettings.Value;
    _logger = logger;
    _letsEncryptService = letsEncryptService;

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
    var cache = default(RegistrationCache);
    if (accountId == null) {
      accountId = Guid.NewGuid();
    }

    var result = await _letsEncryptService.Init(sessionId, requestData.Contacts, cache);
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

    return IDomainResult.Success();
  }

  public (Dictionary<string, string>?, IDomainResult) ApplyCertificates(Guid sessionId, GetCertificatesRequest requestData) {
    var haproxyHelper = new HaproxyCertificateUpdater();

    var result = new Dictionary<string, string>();

    foreach (var subject in requestData.Hostnames) {

      var (cert, getCertResult) = _letsEncryptService.TryGetCachedCertificate(sessionId, subject);
      if (!getCertResult.IsSuccess || cert == null)
        return (null, getCertResult);

      //haproxyHelper.ApplyCertificates(subject, cert.Certificate, cert.PrivateKeyPem);
      var content = $"{cert.Certificate}\n{cert.PrivateKeyPem}";
      result.Add(subject, content);
    }

    return IDomainResult.Success(result);
  }


  public (string?, IDomainResult) AcmeChallenge(string fileName) {

    //var currentDate = DateTime.Now;

    //foreach (var file in Directory.GetFiles(_acmePath)) {
    //  var creationTime = System.IO.File.GetCreationTime(file);

    //  // Calculate the time difference
    //  var timeDifference = currentDate - creationTime;

    //  // If the file is older than 1 day, delete it
    //  if (timeDifference.TotalDays > 1) {
    //    File.Delete(file);
    //    _logger.LogInformation($"Deleted file: {file}");
    //  }
    //}

    var fileContent = File.ReadAllText(Path.Combine(_acmePath, fileName));
    if (fileContent == null)
      return IDomainResult.NotFound<string?>();

    return IDomainResult.Success(fileContent);
  }


}





public class HaproxyCertificateUpdater {
  private readonly string haproxySocketAddress = "192.168.1.4";
  private readonly int haproxySocketPort = 9999;

  public void ApplyCertificates(string subject, string certPem, string keyPem) {
    if (string.IsNullOrEmpty(certPem) || string.IsNullOrEmpty(keyPem)) {
      Console.WriteLine($"Certificate or key for {subject} is invalid");
      return;
    }

    string certFileName = $"/etc/haproxy/certs/{subject}.pem";
    string fullCert = $"{certPem}\n{keyPem}";

    try {
      SendCommand($"new ssl cert {certFileName}");
      SendCommand($"set ssl cert {certFileName} <<\n{fullCert}\n");
      SendCommand($"commit ssl cert {certFileName}");

      Console.WriteLine($"Certificate for {subject} updated successfully");
    }
    catch (Exception ex) {
      Console.WriteLine($"Exception while updating certificate for {subject}: {ex.Message}");
    }
  }

  private void SendCommand(string command) {
    using (var client = new TcpClient(haproxySocketAddress, haproxySocketPort))
    using (var stream = client.GetStream())
    using (var writer = new StreamWriter(stream))
    using (var reader = new StreamReader(stream)) {
      writer.WriteLine(command);
      writer.Flush();

      string response = reader.ReadToEnd();
      if (!response.Contains("Success")) {
        throw new Exception($"Command failed: {response}");
      }
    }
  }
}
