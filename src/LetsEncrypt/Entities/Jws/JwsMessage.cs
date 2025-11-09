using System.Text.Json.Serialization;


namespace MaksIT.LetsEncrypt.Entities.Jws;

public class JwsMessage {

  public string? Protected { get; set; }

  public string? Payload { get; set; }

  public string? Signature { get; set; }
}


public class JwsHeader {

  [JsonPropertyName("alg")]
  public string? Algorithm { get; set; }

  [JsonPropertyName("jwk")]
  public Jwk? Key { get; set; }


  [JsonPropertyName("kid")]
  public string? KeyId { get; set; }

  public string? Nonce { get; set; }

  public Uri? Url { get; set; }


  [JsonPropertyName("Host")]
  public string? Host { get; set; }
}
