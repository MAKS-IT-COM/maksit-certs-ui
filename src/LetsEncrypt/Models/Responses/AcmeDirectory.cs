using System;

namespace MaksIT.LetsEncrypt.Models.Responses
{
    public class AcmeDirectory
    {
        public Uri NewNonce { get; set; }


        public Uri NewAccount { get; set; }

        public Uri NewOrder { get; set; }

        // New authorization If the ACME server does not implement pre-authorization
        // (Section 7.4.1) it MUST omit the "newAuthz" field of the directory.
        // [JsonProperty("newAuthz")]
        // public Uri NewAuthz { get; set; }
        public Uri RevokeCertificate { get; set; }

        public Uri KeyChange { get; set; }

        public AcmeDirectoryMeta Meta { get; set; }
    }

    public class AcmeDirectoryMeta
    {
        public string TermsOfService { get; set; }
    }
}
