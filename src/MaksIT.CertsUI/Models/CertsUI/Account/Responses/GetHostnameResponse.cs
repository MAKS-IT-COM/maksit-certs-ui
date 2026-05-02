using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.Account.Responses;

public class GetHostnameResponse : ResponseModelBase {
  public required string Hostname { get; set; }
  public DateTime Expires { get; set; }
  public bool IsUpcomingExpire { get; set; }
  public bool IsDisabled { get; set; }
}
