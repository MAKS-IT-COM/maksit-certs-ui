using DomainResults.Common;
using Microsoft.Extensions.Logging;

using Renci.SshNet;
using Renci.SshNet.Common;
using System.Text.RegularExpressions;

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
      _logger = logger;
      _sshClient = new SshClient(host, port, username, password);
      _sftpClient = new SftpClient(host, port, username, password);
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


        var shellStream = _sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024, new Dictionary<TerminalModes, uint> {
          { TerminalModes.ECHO, 53 }
        });

        //Get logged in
        string rep = shellStream.Expect(new Regex(@"[$>]")); //expect user prompt
        //this.writeOutput(results, rep);
        _logger.LogInformation(rep);

        //send command
        shellStream.WriteLine(command);
        rep = shellStream.Expect(new Regex(@"([$#>:])")); //expect password or user prompt
        _logger.LogInformation(rep);

        //check to send password
        if (rep.Contains(":")) {
          //send password
          shellStream.WriteLine(password);
          rep = shellStream.Expect(new Regex(@"[$#>]")); //expect user or root prompt
          _logger.LogInformation(rep);
        }

        return IDomainResult.Success();
      }
      catch (Exception ex) {
        _logger.LogError(ex, "SSH Service unhandled exeption");
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
