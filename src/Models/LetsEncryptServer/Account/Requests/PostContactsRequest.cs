using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Account.Requests {
  public class PostContactsRequest : IValidatableObject {

      public required string[] Contacts { get; set; }

      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {

        if (Contacts == null || Contacts.Length == 0)
          yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });
      }
    }
  }