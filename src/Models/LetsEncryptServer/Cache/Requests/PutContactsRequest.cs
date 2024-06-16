using System.ComponentModel.DataAnnotations;


namespace MaksIT.Models.LetsEncryptServer.Cache.Requests {
  public class PutContactsRequest : IValidatableObject {

    public required string[] Contacts { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {

      if (Contacts == null || Contacts.Length == 0)
        yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });
    }
  }
}
