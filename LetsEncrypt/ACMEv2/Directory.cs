using System;
using Newtonsoft.Json;

namespace ACMEv2
{
    public class Directory
    {
        //New nonce 
        [JsonProperty("newNonce")]
        public Uri NewNonce { get; set; }

        //New account 
        [JsonProperty("newAccount")]
        public Uri NewAccount { get; set; }

        //New order
        [JsonProperty("newOrder")]
        public Uri NewOrder { get; set; }

        // New authorization If the ACME server does not implement pre-authorization
        // (Section 7.4.1) it MUST omit the "newAuthz" field of the directory.
        // [JsonProperty("newAuthz")]
        // public Uri NewAuthz { get; set; }

        //Revoke certificate
        [JsonProperty("revokeCert")]
        public Uri RevokeCertificate { get; set; }

        //Key change
        [JsonProperty("keyChange")]
        public Uri KeyChange { get; set; }

        //Metadata object
        [JsonProperty("meta")]
        public DirectoryMeta Meta { get; set; }
    }
}
