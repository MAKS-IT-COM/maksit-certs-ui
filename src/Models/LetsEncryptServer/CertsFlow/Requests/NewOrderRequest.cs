using System.ComponentModel.DataAnnotations;

namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests
{
  public class NewOrderRequest : IValidatableObject {
        public required string[] Hostnames { get; set; }

        public required string ChallengeType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });

      if (string.IsNullOrWhiteSpace(ChallengeType) && ChallengeType != "http-01")
        yield return new ValidationResult("ChallengeType is required", new[] { nameof(ChallengeType) });
    }
  }
}
