using System.ComponentModel.DataAnnotations;

namespace MaksIT.LetsEncrypt.Entities
{
    public enum ContentType
    {
        [Display(Name = "application/jose+json")]
        JoseJson,
        [Display(Name = "application/problem+json")]
        ProblemJson,
        [Display(Name = "application/pem-certificate-chain")]
        PemCertificateChain,
        [Display(Name = "application/json")]
        Json
    }

    public static class ContentTypeExtensions
    {
        public static string GetDisplayName(this ContentType contentType)
        {
            var type = typeof(ContentType);
            var memInfo = type.GetMember(contentType.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            return attributes.Length > 0 ? ((DisplayAttribute)attributes[0]).Name : contentType.ToString();
        }
    }
}
