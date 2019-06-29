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
    public class AuthorizationChallengeResponse
    {
        [JsonProperty("identifier")]
        public OrderIdentifier Identifier { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        [JsonProperty("wildcard")]
        public bool Wildcard { get; set; }

        [JsonProperty("challenges")]
        public AuthorizationChallenge[] Challenges { get; set; }
    }


}
