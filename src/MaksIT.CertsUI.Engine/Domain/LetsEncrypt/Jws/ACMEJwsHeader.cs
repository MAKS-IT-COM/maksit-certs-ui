using MaksIT.Core.Security.JWS;
using System.Text.Json.Serialization;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt.Jws;

public class ACMEJwsHeader : JwsHeader
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }
}
