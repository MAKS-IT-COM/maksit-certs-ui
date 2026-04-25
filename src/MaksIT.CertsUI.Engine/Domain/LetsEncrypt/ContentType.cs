using System.ComponentModel.DataAnnotations;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

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
