using System.Security.Cryptography;

namespace LetsEncrypt.Entities
{
    public class CachedCertificateResult
    {
        public RSACryptoServiceProvider PrivateKey;
        public string Certificate;
    }

}
