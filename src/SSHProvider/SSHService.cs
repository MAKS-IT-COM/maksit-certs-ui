using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using DomainResults.Common;

using Renci.SshNet;
using Renci.SshNet.Common;

namespace MaksIT.SSHProvider {

  public interface ISSHService : IDisposable {
    IDomainResult Upload(string workingdirectory, string fileName, byte[] bytes);

    IDomainResult ListDir(string workingdirectory);

    IDomainResult Download();
  }

  public class SSHService : ISSHService {

    public readonly ILogger _logger;

    public readonly SshClient _sshClient;
    public readonly SftpClient _sftpClient;

    public SSHService(
      ILogger logger,
      string host,
      int port,
      string username,
      string password
    ) {

      if(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        throw new ArgumentNullException($"{nameof(username)} or {nameof(password)} is null, empty or white space");

      _logger = logger;
      _sshClient = new SshClient(host, port, username, password);
      _sftpClient = new SftpClient(host, port, username, password);
    }


    public SSHService(
      ILogger logger,
      string host,
      int port,
      string username,
      string [] privateKeys
    ) {

      if (string.IsNullOrWhiteSpace(username) || privateKeys.Any(x => string.IsNullOrWhiteSpace(x)))
        throw new ArgumentNullException($"{nameof(username)} or {nameof(privateKeys)} contains key which is null, empty or white space");

      _logger = logger;

      var privateKeyFiles = new List<PrivateKeyFile>();
      foreach (var privateKey in privateKeys) {
        using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(privateKey))) {
          privateKeyFiles.Add(new PrivateKeyFile(ms));
        }
      }

      _sshClient = new SshClient(host, port, username, privateKeyFiles.ToArray());
      _sftpClient = new SftpClient(host, port, username, privateKeyFiles.ToArray());
    }

    public IDomainResult Connect() {
      try {
        _sshClient.Connect();
        _sftpClient.Connect();

        return IDomainResult.Success();
      }
      catch (Exception ex){
        _logger.LogError(ex, "SSH Service unhandled exeption");
        return IDomainResult.CriticalDependencyError();
      }
    }

    public IDomainResult Upload(string workingdirectory, string fileName, byte[] bytes) {

      try {
        _sftpClient.ChangeDirectory(workingdirectory);
        _logger.LogInformation($"Changed directory to {workingdirectory}");

        using var memoryStream = new MemoryStream(bytes);

        _logger.LogInformation($"Uploading {fileName} ({memoryStream.Length:N0} bytes)");

        _sftpClient.BufferSize = 4 * 1024; // bypass Payload error large files
        _sftpClient.UploadFile(memoryStream, fileName);

        return IDomainResult.Success();
      }
      catch (Exception ex) {
        _logger.LogError(ex, "SSH Service unhandled exeption");
        return IDomainResult.CriticalDependencyError();
      }
    }

    public IDomainResult ListDir(string workingdirectory) {
      try {

        var listDirectory = _sftpClient.ListDirectory(workingdirectory);
        
        _logger.LogInformation($"Listing directory:");

        foreach (var file in listDirectory) {
          _logger.LogInformation($" - " + file.Name);
        }

        return IDomainResult.Success();
      }

      catch (Exception ex) {
        _logger.LogError(ex, "SSH Service unhandled exeption");
        return IDomainResult.CriticalDependencyError();
      }
}

    public IDomainResult Download() {
      return IDomainResult.Failed();
    }

    public IDomainResult RunSudoCommand(string password, string command) {
      try {
        command = $"sudo {command}";

        using (var shellStream = _sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024, new Dictionary<TerminalModes, uint> {
            { TerminalModes.ECHO, 53 }
        })) {
          // Get logged in
          string rep = shellStream.Expect(new Regex(@"[$>]"), TimeSpan.FromSeconds(10)); // expect user prompt with timeout
          _logger.LogInformation("Initial prompt: {Prompt}", rep);

          // Send command
          shellStream.WriteLine(command);
          rep = shellStream.Expect(new Regex(@"([$#>:])"), TimeSpan.FromSeconds(10)); // expect password or user prompt with timeout
          _logger.LogInformation("After command prompt: {Prompt}", rep);

          // Check to send password
          if (rep.Contains(":")) {
            // Send password
            shellStream.WriteLine(password);
            rep = shellStream.Expect(new Regex(@"[$#>]"), TimeSpan.FromSeconds(10)); // expect user or root prompt with timeout
            _logger.LogInformation("After password prompt: {Prompt}", rep);
          }

          return IDomainResult.Success();
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex, "SSH Service unhandled exception");
        return IDomainResult.CriticalDependencyError();
      }
    }

    public void Dispose() {
      _sshClient.Disconnect();
      _sshClient.Dispose();

      _sftpClient.Disconnect();
      _sftpClient.Dispose();
    }
  }
}
