namespace MaksIT.CertsUI.Contracts;

public class AccountResponse {
  public Guid AccountId { get; set; }
  public required bool IsDisabled { get; set; }
  public string? Description { get; set; }
  public required string[] Contacts { get; set; }
  public string? ChallengeType { get; set; }
  public HostnameResponse[]? Hostnames { get; set; }
  public required bool IsStaging { get; set; }
}
