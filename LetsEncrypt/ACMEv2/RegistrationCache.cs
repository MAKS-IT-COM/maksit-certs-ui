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
    public class RegistrationCache
    {
        public readonly Dictionary<string, CertificateCache> CachedCerts = new Dictionary<string, CertificateCache>(StringComparer.OrdinalIgnoreCase);
        public byte[] AccountKey;
        public string Id;
        public Jwk Key;
        public Uri Location;
    }



}
