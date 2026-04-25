using System.ComponentModel.DataAnnotations;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

public enum ChalengeType
{
    [Display(Name = "http-01")]
    http,
    [Display(Name = "dns-01")]
    dns,
}
