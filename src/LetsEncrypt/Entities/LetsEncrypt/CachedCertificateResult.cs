using System.Security.Cryptography;

namespace MaksIT.LetsEncrypt.Entities
{
    public class CachedCertificateResult
    {
        public RSACryptoServiceProvider? PrivateKey { get; set; }
        public string? Certificate { get; set; }
    }

}
