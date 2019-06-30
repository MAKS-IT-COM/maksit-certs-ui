using System.Security.Cryptography;

namespace ACMEv2
{
    public class CachedCertificateResult
    {
        public RSACryptoServiceProvider PrivateKey;
        public string Certificate;
    }

}
