using MaksIT.LetsEncrypt;

namespace MaksIT.LetsEncryptServer {

  public class Agent {

    private string? _agentHostname;
    public string AgentHostname {
      get {
        var env = Environment.GetEnvironmentVariable("MAKS-IT_AGENT_HOSTNAME");
        return env ?? _agentHostname ?? string.Empty;
      }
      set {
        _agentHostname = value;
      }
    }

    private int? _agentPort;
    public int AgentPort {
      get {
        var env = Environment.GetEnvironmentVariable("MAKS-IT_AGENT_PORT");
        return env != null ? int.Parse(env) : _agentPort ?? 0;
      }
      set {
        _agentPort = value;
      }

    }

    private string? _agentKey;
    public string AgentKey {
      get {
        var env = Environment.GetEnvironmentVariable("MAKS-IT_AGENT_KEY");
        return env ?? _agentKey ?? string.Empty;
      }
      set {
        _agentKey = value;
      }
    }

    private string? _serviceToReload;
    public string ServiceToReload {
      get {
        var env = Environment.GetEnvironmentVariable("MAKS-IT_AGENT_SERVICE");
        return env ?? _serviceToReload ?? string.Empty;
      }
      set {
        _serviceToReload = value;
      }
    }
  }






  public class Configuration : ILetsEncryptConfiguration {

    private string? _production;
    public string Production {
      get {
        var env = Environment.GetEnvironmentVariable("LETSENCRYPT_SERVER_PRODUCTION");
        return env ?? _production ?? string.Empty;
      }
      set {
        _production = value;
      }

    }

    private string? _staging;
    public string Staging {
      get {
        var env = Environment.GetEnvironmentVariable("LETSENCRYPT_SERVER_STAGING");
        return env ?? _staging ?? string.Empty;
      }
      set {
        _staging = value;
      }
    }

    public required Agent Agent { get; set; }
  }
}
