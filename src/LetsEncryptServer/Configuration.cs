namespace MaksIT.LetsEncryptServer {

  public class Agent {
    public required string AgentHostname { get; set; }
    public required int AgentPort { get; set; }
    public required string AgentKey { get; set; }

    public required string ServiceToReload { get; set; }
  }

  public class Configuration {    
    public required string Production { get; set; }
    public required string Staging { get; set; }
    public required Agent Agent { get; set; }
  }
}
