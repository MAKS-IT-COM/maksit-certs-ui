using System.ComponentModel.DataAnnotations;


namespace MaksIT.LetsEncrypt.Entities;

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