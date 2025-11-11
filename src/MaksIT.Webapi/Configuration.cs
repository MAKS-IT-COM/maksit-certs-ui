using MaksIT.LetsEncrypt;

namespace MaksIT.Webapi {

  public class Agent {
    public required string AgentHostname { get; set; }
    public required int AgentPort { get; set; }
    public required string AgentKey { get; set; }
    public required string ServiceToReload { get; set; }
  }

  public class Auth {

    public required string Secret { get; set; }

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required int Expiration { get; set; }

    public required int RefreshExpiration { get; set; }


    public required string Pepper { get; set; }

  }

  public class Configuration : ILetsEncryptConfiguration {
    public required Auth Auth { get; set; }

    public required string SettingsFile { get; set; }

    public required string Production { get; set; }
    public required string Staging { get; set; }

    public required string CacheFolder { get; set; }
    public required string AcmeFolder { get; set; }
    public required string DataFolder { get; set; }

    public required Agent Agent { get; set; }
  }
}
