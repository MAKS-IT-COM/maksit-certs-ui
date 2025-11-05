using MaksIT.Core.Abstractions.Webapi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests {
  public class RevokeCertificatesRequest : RequestModelBase {
    
    public required string [] Hostnames { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
      if (Hostnames == null || Hostnames.Length == 0)
        yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });
    }
  }
}
