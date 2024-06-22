using System.ComponentModel.DataAnnotations;

namespace MaksIT.Models.LetsEncryptServer.Cache.Requests {
  public class PostAccountRequest : IValidatableObject {
    public required string Description { get; set; }
    public required string[] Contacts { get; set; }
    public required string[] Hostnames { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (string.IsNullOrWhiteSpace(Description))
        yield return new ValidationResult("Description is required", new[] { nameof(Description) });

      if (Contacts == null || Contacts.Length == 0)
        yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });

      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
    }
  }
}
