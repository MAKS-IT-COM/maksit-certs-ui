/*
 * https://tools.ietf.org/html/draft-ietf-acme-acme-18#section-7.3
 */

using System;
using Newtonsoft.Json;
using LetsEncrypt.Exceptions;

namespace LetsEncrypt.Entities
{
    interface IHasLocation
    {
        Uri Location { get; set; }
    }

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
