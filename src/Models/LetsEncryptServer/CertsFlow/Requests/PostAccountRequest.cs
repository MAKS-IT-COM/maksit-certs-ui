using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests {
  public class PostAccountRequest : IValidatableObject {
    public required string Description { get; set; }
    public required string[] Contacts { get; set; }
    public required string [] Hostnames { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (Description == null || Description.Length == 0)
        yield return new ValidationResult("Description is required", new[] { nameof(Description) });

      if (Contacts == null || Contacts.Length == 0)
        yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });

      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
    }
  }
}
