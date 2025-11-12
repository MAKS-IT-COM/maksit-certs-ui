using MaksIT.Core.Security.JWS;
using System.Text.Json.Serialization;

namespace MaksIT.LetsEncrypt.Entities.Jws;
public class ACMEJwsHeader : JwsHeader {
  [JsonPropertyName("url")]
  public string? Url { get; set; }

  [JsonPropertyName("nonce")]
  public string? Nonce { get; set; }
}
