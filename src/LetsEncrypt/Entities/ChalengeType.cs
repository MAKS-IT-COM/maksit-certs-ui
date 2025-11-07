using System.ComponentModel.DataAnnotations;


namespace MaksIT.LetsEncrypt.Entities;

public enum ChalengeType {
  [Display(Name = "http-01")]
  http,
  [Display(Name = "dns-01")]
  dns,
}