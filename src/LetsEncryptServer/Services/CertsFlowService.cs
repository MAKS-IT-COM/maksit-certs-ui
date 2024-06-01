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

    var notifyResult = NotifyHaproxy(results.Select(x => x.Key));
    if (!notifyResult.IsSuccess)
      return (null, notifyResult);

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


  /**
  abort ssl cert <certfile>               : abort a transaction for a certificate file
  add acl [@<ver>] <acl> <pattern>        : add an acl entry
  add map [@<ver>] <map> <key> <val>      : add a map entry (payload supported instead of key/val)
  add ssl crt-list <list> <cert> [opts]*  : add to crt-list file <list> a line <cert> or a payload
  clear acl [@<ver>] <acl>                : clear the contents of this acl
  clear counters [all]                    : clear max statistics counters (or all counters)
  clear map [@<ver>] <map>                : clear the contents of this map
  clear table <table> [<filter>]*         : remove an entry from a table (filter: data/key)
  commit acl @<ver> <acl>                 : commit the ACL at this version
  commit map @<ver> <map>                 : commit the map at this version
  commit ssl cert <certfile>              : commit a certificate file
  del acl <acl> [<key>|#<ref>]            : delete acl entries matching <key>
  del map <map> [<key>|#<ref>]            : delete map entries matching <key>
  del ssl cert <certfile>                 : delete an unused certificate file
  del ssl crt-list <list> <cert[:line]>   : delete a line <cert> from crt-list file <list>
  disable agent                           : disable agent checks
  disable dynamic-cookie backend <bk>     : disable dynamic cookies on a specific backend
  disable frontend <frontend>             : temporarily disable specific frontend
  disable health                          : disable health checks
  disable server (DEPRECATED)             : disable a server for maintenance (use 'set server' instead)
  enable agent                            : enable agent checks
  enable dynamic-cookie backend <bk>      : enable dynamic cookies on a specific backend
  enable frontend <frontend>              : re-enable specific frontend
  enable health                           : enable health checks
  enable server  (DEPRECATED)             : enable a disabled server (use 'set server' instead)
  get acl <acl> <value>                   : report the patterns matching a sample for an ACL
  get map <acl> <value>                   : report the keys and values matching a sample for a map
  get var <name>                          : retrieve contents of a process-wide variable
  get weight <bk>/<srv>                   : report a server's current weight
  new ssl cert <certfile>                 : create a new certificate file to be used in a crt-list or a directory
  operator                                : lower the level of the current CLI session to operator
  prepare acl <acl>                       : prepare a new version for atomic ACL replacement
  prepare map <acl>                       : prepare a new version for atomic map replacement
  set dynamic-cookie-key backend <bk> <k> : change a backend secret key for dynamic cookies
  set map <map> [<key>|#<ref>] <value>    : modify a map entry
  set maxconn frontend <frontend> <value> : change a frontend's maxconn setting
  set maxconn global <value>              : change the per-process maxconn setting
  set maxconn server <bk>/<srv>           : change a server's maxconn setting
  set profiling <what> {auto|on|off}      : enable/disable resource profiling (tasks,memory)
  set rate-limit <setting> <value>        : change a rate limiting value
  set server <bk>/<srv> [opts]            : change a server's state, weight, address or ssl
  set severity-output [none|number|string]: set presence of severity level in feedback information
  set ssl cert <certfile> <payload>       : replace a certificate file
  set ssl ocsp-response <resp|payload>    : update a certificate's OCSP Response from a base64-encode DER
  set ssl tls-key [id|file] <key>         : set the next TLS key for the <id> or <file> listener to <key>
  set table <table> key <k> [data.* <v>]* : update or create a table entry's data
  set timeout [cli] <delay>               : change a timeout setting
  set weight <bk>/<srv>  (DEPRECATED)     : change a server's weight (use 'set server' instead)
  show acl [@<ver>] <acl>]                : report available acls or dump an acl's contents
  show activity                           : show per-thread activity stats (for support/developers)
  show backend                            : list backends in the current running config
  show cache                              : show cache status
  show cli level                          : display the level of the current CLI session
  show cli sockets                        : dump list of cli sockets
  show env [var]                          : dump environment variables known to the process
  show errors [<px>] [request|response]   : report last request and/or response errors for each proxy
  show events [<sink>] [-w] [-n]          : show event sink state
  show fd [num]                           : dump list of file descriptors in use or a specific one
  show info [desc|json|typed|float]*      : report information about the running process
  show libs                               : show loaded object files and libraries
  show map [@ver] [map]                   : report available maps or dump a map's contents
  show peers [dict|-] [section]           : dump some information about all the peers or this peers section
  show pools                              : report information about the memory pools usage
  show profiling [<what>|<#lines>|byaddr]*: show profiling state (all,status,tasks,memory)
  show resolvers [id]                     : dumps counters from all resolvers section and associated name servers
  show schema json                        : report schema used for stats
  show servers conn [<backend>]           : dump server connections status (all or for a single backend)
  show servers state [<backend>]          : dump volatile server information (all or for a single backend)
  show sess [id]                          : report the list of current sessions or dump this exact session
  show ssl cert [<certfile>]              : display the SSL certificates used in memory, or the details of a file
  show ssl crt-list [-n] [<list>]         : show the list of crt-lists or the content of a crt-list file <list>
  show startup-logs                       : report logs emitted during HAProxy startup
  show stat [desc|json|no-maint|typed|up]*: report counters for each proxy and server
  show table <table> [<filter>]*          : report table usage stats or dump this table's contents (filter: data/key)
  show tasks                              : show running tasks
  show threads                            : show some threads debugging information
  show tls-keys [id|*]                    : show tls keys references or dump tls ticket keys when id specified
  show trace [<module>]                   : show live tracing state
  show version                            : show version of the current process
  shutdown frontend <frontend>            : stop a specific frontend
  shutdown session [id]                   : kill a specific session
  shutdown sessions server <bk>/<srv>     : kill sessions on a server
  trace [<module>|0] [cmd [args...]]      : manage live tracing (empty to list, 0 to stop all)
  user                                    : lower the level of the current CLI session to user
  help [<command>]                        : list matching or all commands
  prompt                                  : toggle interactive mode with prompt
  quit                                    : disconnect
  */
  private IDomainResult NotifyHaproxy(IEnumerable<string> certFiles) {
    var server = _appSettings.Server;
    try {
      using (var client = new TcpClient(server.Ip, server.SocketPort))
      using (var networkStream = client.GetStream())
      using (var writer = new StreamWriter(networkStream, Encoding.ASCII))
      using (var reader = new StreamReader(networkStream, Encoding.ASCII)) {
        writer.AutoFlush = true;

        foreach (var certFile in certFiles) {

          // Prepare the certificate
          string prepareCommand = $"new ssl cert {server.Path}/{certFile}\n";
          writer.WriteLine(prepareCommand);
          string prepareResponse = reader.ReadLine();
          if (prepareResponse.Contains("error", StringComparison.OrdinalIgnoreCase)) {
            _logger.LogError($"Error while preparing certificate {certFile}: {prepareResponse}");
            return IDomainResult.CriticalDependencyError($"Error while preparing certificate {certFile}");
          }

          // Commit the certificate
          string commitCommand = $"commit ssl cert {server.Path}/{certFile}\n";
          writer.WriteLine(commitCommand);
          string commitResponse = reader.ReadLine();
          if (commitResponse.Contains("error", StringComparison.OrdinalIgnoreCase)) {
            _logger.LogError($"Error while committing certificate {certFile}: {commitResponse}");
            return IDomainResult.CriticalDependencyError($"Error while committing certificate {certFile}");
          }
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
