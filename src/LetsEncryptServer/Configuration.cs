namespace MaksIT.LetsEncryptServer {

  public class Server {
    public required string Ip { get; set; }
    public required int SocketPort { get; set; }
    public required int SSHPort { get; set; }
    public required string Path { get; set; }

    public required string Username { get; set; }
    public string? Password { get; set; }
    public string[]? PrivateKeys { get; set; }
  }

  public class Configuration {    
    public required string Production { get; set; }
    public required string Staging { get; set; }
    public required bool DevMode { get; set; }
    public required Server Server { get; set; }
  }
}
