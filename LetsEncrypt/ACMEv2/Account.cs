/*
 * https://tools.ietf.org/html/draft-ietf-acme-acme-18#section-7.3
 */

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
    public class Account : IHasLocation
    {
        [JsonProperty("termsOfServiceAgreed")]
        public bool TermsOfServiceAgreed { get; set; }

        /*
        onlyReturnExisting (optional, boolean):  If this field is present
        with the value "true", then the server MUST NOT create a new
        account if one does not already exist.  This allows a client to
        look up an account URL based on an account key
        */
        [JsonProperty("onlyReturnExisting")]
        public bool OnlyReturnExisting { get; set; }

        [JsonProperty("contact")]
        public string[] Contacts { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("key")]
        public Jwk Key { get; set; }

        [JsonProperty("initialIp")]
        public string InitialIp { get; set; }

        [JsonProperty("orders")]
        public Uri Orders { get; set; }

        public Uri Location { get; set; }
    }

}
