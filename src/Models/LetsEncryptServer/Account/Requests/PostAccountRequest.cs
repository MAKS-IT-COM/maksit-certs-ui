using System.ComponentModel.DataAnnotations;
using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.Account.Requests;

public class PostAccountRequest : RequestModelBase {
  public required string Description { get; set; }
  public required string[] Contacts { get; set; }
  public required string ChallengeType { get; set; }
  public required string[] Hostnames { get; set; }
  public required bool IsStaging { get; set; }
  public required bool AgreeToS { get; set; }

  public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    if (string.IsNullOrWhiteSpace(Description))
      yield return new ValidationResult("Description is required", [nameof(Description)]);

    if (Contacts == null || Contacts.Length == 0)
      yield return new ValidationResult("Contacts is required", [nameof(Contacts)]);

    if (Hostnames == null || Hostnames.Length == 0)
      yield return new ValidationResult("Hostnames is required", [nameof(Hostnames)]);

    if (string.IsNullOrWhiteSpace(ChallengeType) && ChallengeType != "http-01")
      yield return new ValidationResult("ChallengeType is required", [nameof(ChallengeType)]);

    if (!AgreeToS)
      yield return  new ValidationResult("You must agree to the Terms of Service", [nameof(AgreeToS)]);
  }
}
