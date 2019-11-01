using System;
using Newtonsoft.Json;

namespace ACMEv2
{
    public class AuthorizationChallenge
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

    }

}
