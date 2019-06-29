using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ACMEv2
{
    public class Order : IHasLocation
    {
        public Uri Location { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        [JsonProperty("identifiers")]
        public OrderIdentifier[] Identifiers { get; set; }

        [JsonProperty("notBefore")]
        public DateTime? NotBefore { get; set; }

        [JsonProperty("notAfter")]
        public DateTime? NotAfter { get; set; }

        [JsonProperty("error")]
        public Problem Error { get; set; }

        [JsonProperty("authorizations")]
        public Uri[] Authorizations { get; set; }

        [JsonProperty("finalize")]
        public Uri Finalize { get; set; }

        [JsonProperty("certificate")]
        public Uri Certificate { get; set; }
    }

}
