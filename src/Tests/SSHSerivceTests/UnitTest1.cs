using System.Security.Cryptography;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MaksIT.SSHProvider;

using MaksIT.Tests.SSHProviderTests.Abstractions;

namespace MaksIT.SSHSerivceTests; 
public class UnitTest1 : ServicesBase {

  public readonly string _appPath = AppDomain.CurrentDomain.BaseDirectory;

  [Fact]
  public void UploadFile() {

    var username = "";
    var password = "";
    var filePath = Path.Combine(_appPath, "randomfile.txt");
    CreateRandomFile(filePath, 1);

    var logger = ServiceProvider.GetService<ILogger<SSHService>>();

    using var sshService = new SSHService(logger, "192.168.0.10", 22, username, password);
    sshService.Connect();

    var bytes = File.ReadAllBytes(filePath);

    logger.LogInformation($"Uploading {filePath} ({bytes.Length:N0} bytes)");

    sshService.RunSudoCommand(password, "chown nginx:nginx /var/www/ssl -R");
    sshService.RunSudoCommand(password, "chmod 777 /var/www/ssl -R");

    sshService.Upload("/var/www/ssl", Path.GetFileName(filePath), bytes);

    sshService.RunSudoCommand(password, "chown nginx:nginx /var/www/ssl -R");
    sshService.RunSudoCommand(password, "chmod 775 /var/www/ssl -R");
  }

  private void CreateRandomFile(string filePath, int sizeInMb) {
    // Note: block size must be a factor of 1MB to avoid rounding errors
    const int blockSize = 1024 * 8;
    const int blocksPerMb = (1024 * 1024) / blockSize;

    byte[] data = new byte[blockSize];

    using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider()) {
      using (FileStream stream = File.OpenWrite(filePath)) {
        for (int i = 0; i < sizeInMb * blocksPerMb; i++) {
          crypto.GetBytes(data);
          stream.Write(data, 0, data.Length);
        }
      }
    }
  }
}
