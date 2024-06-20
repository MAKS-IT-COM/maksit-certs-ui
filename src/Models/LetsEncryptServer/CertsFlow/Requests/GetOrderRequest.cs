using System.ComponentModel.DataAnnotations;

namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests
{
  public class GetOrderRequest : IValidatableObject {
    public required string[] Hostnames { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
    }
  }
}
