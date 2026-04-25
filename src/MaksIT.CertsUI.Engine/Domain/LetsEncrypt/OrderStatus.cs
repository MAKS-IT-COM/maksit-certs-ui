using System.ComponentModel.DataAnnotations;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

public enum OrderStatus
{
    [Display(Name = "pending")]
    Pending,
    [Display(Name = "valid")]
    Valid,
    [Display(Name = "ready")]
    Ready,
    [Display(Name = "processing")]
    Processing
}
