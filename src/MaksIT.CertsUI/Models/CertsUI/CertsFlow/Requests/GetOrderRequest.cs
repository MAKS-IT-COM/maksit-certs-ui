using System.ComponentModel.DataAnnotations;
using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.CertsFlow.Requests;

public class GetOrderRequest : RequestModelBase {
  public required string[] Hostnames { get; set; }

  public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (Hostnames == null || Hostnames.Length == 0)
      yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
  }
}
