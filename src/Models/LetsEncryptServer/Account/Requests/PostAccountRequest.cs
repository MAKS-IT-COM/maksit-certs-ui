using MaksIT.Core.Abstractions.Webapi;
using System.ComponentModel.DataAnnotations;

namespace MaksIT.Models.LetsEncryptServer.Account.Requests;
public class PostAccountRequest : RequestModelBase {
  public required string Description { get; set; }
  public required string[] Contacts { get; set; }
  public required string ChallengeType { get; set; }
  public required string[] Hostnames { get; set; }
  public required bool IsStaging { get; set; }

  public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (string.IsNullOrWhiteSpace(Description))
      yield return new ValidationResult("Description is required", new[] { nameof(Description) });

    if (Contacts == null || Contacts.Length == 0)
      yield return new ValidationResult("Contacts is required", new[] { nameof(Contacts) });

    if (Hostnames == null || Hostnames.Length == 0)
      yield return new ValidationResult("Hostnames is required", new[] { nameof(Hostnames) });

    if (string.IsNullOrWhiteSpace(ChallengeType) && ChallengeType != "http-01")
      yield return new ValidationResult("ChallengeType is required", new[] { nameof(ChallengeType) });
  }
}
