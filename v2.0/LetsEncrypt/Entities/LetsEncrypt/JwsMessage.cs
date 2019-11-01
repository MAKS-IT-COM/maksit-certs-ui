using System;
using Newtonsoft.Json;


namespace LetsEncrypt.Entities
{

    public class JwsMessage
    {
        [JsonProperty("header")]
        public JwsHeader Header { get; set; }

        [JsonProperty("protected")]
        public string Protected { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }


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
