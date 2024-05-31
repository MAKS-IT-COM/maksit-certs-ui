namespace MaksIT.LetsEncryptServer {

  public class SSHClientConfing {
    public required string User {  get; set; }
    public required string Key { get; set; }
  }

  public class Server {
    public required string Ip { get; set; }
    public required int Port { get; set; }
    public string Path { get; set; }
    public required SSHClientConfing SSH { get; set; }
  }

  public class Configuration {    
    public required string Production { get; set; }
    public required string Staging { get; set; }
    public required bool DevMode { get; set; }
    public required Server Server { get; set; }
  }
}
