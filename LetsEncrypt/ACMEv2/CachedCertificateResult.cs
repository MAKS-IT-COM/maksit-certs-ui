using System.Security.Cryptography;

namespace ACMEv2
{
    public class CachedCertificateResult
    {
        public RSA PrivateKey;
        public string Certificate;
    }

}
