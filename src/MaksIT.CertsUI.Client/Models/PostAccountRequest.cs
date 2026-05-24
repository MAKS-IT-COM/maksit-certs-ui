namespace MaksIT.CertsUI.Client.Models;

public class PostAccountRequest {
  public required string Description { get; set; }
  public required string[] Contacts { get; set; }
  public required string ChallengeType { get; set; }
  public required string[] Hostnames { get; set; }
  public required bool IsStaging { get; set; }
  public required bool AgreeToS { get; set; }
}
