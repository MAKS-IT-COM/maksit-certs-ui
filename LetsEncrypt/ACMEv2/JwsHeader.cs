using System;
using Newtonsoft.Json;

namespace ACMEv2
{
    public class JwsHeader
    {
        //public JwsHeader()
        //{
        //}

        //public JwsHeader(string algorithm, Jwk key)
        //{
        //    Algorithm = algorithm;
        //    Key = key;
        //}

        [JsonProperty("alg")]
        public string Algorithm { get; set; }

        [JsonProperty("jwk")]
        public Jwk Key { get; set; }


        [JsonProperty("kid")]
        public string KeyId { get; set; }


        [JsonProperty("nonce")]
        public string Nonce { get; set; }


        [JsonProperty("url")]
        public Uri Url { get; set; }


        [JsonProperty("Host")]
        public string Host { get; set; }
    }
}
