namespace MaksIT.LetsEncrypt.Entities;

public class CachedHostname(
  string hostname,
  DateTime expires,
  bool isUpcomingExpire,
  bool isDisabled
) {
  public string Hostname { get; set; } = hostname;
  public DateTime Expires { get; set; } = expires;
  public bool IsUpcomingExpire { get; set; } = isUpcomingExpire;
  public bool IsDisabled { get; set; } = isDisabled;
}
