using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.Account.Responses;

public class GetAccountResponse : ResponseModelBase {
  public Guid AccountId { get; set; }
  public required bool IsDisabled { get; set; }
  public string? Description { get; set; }
  public required string[] Contacts { get; set; }
  public string? ChallengeType { get; set; }
  public GetHostnameResponse[]? Hostnames { get; set; }
  public required bool IsStaging { get; set; }
}
