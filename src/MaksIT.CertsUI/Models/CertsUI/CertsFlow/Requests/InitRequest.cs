using System.ComponentModel.DataAnnotations;
using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.CertsFlow.Requests;

public class InitRequest : RequestModelBase {
  public required string Description { get; set; }
  public required string[] Contacts { get; set; }

  public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (string.IsNullOrWhiteSpace(Description))
      yield return new ValidationResult("Description is required", new[] { nameof(Description) });

    if (Contacts == null || Contacts.Length == 0)
      yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });
  }
}
