using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Account.Requests {
  public class PostHostnamesRequest : IValidatableObject {
    public required string[] Hostnames { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
    }
  }
}
