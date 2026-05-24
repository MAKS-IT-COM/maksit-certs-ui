namespace MaksIT.CertsUI.Contracts;

public class HostnameResponse {
  public required string Hostname { get; set; }
  public DateTime Expires { get; set; }
  public bool IsUpcomingExpire { get; set; }
  public bool IsDisabled { get; set; }
}
