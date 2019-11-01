using System;
using System.Collections.Generic;

namespace ACMEv2
{
    public class RegistrationCache
    {
        public readonly Dictionary<string, CertificateCache> CachedCerts = new Dictionary<string, CertificateCache>(StringComparer.OrdinalIgnoreCase);
        public byte[] AccountKey;
        public string Id;
        public Jwk Key;
        public Uri Location;
    }



}
