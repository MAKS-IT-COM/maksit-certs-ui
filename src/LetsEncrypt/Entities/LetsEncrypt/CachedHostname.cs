namespace MaksIT.LetsEncrypt.Entities;

public class CachedHostname {
  public string Hostname { get; set; }
  public DateTime Expires { get; set; }
  public bool IsUpcomingExpire { get; set; }

  public bool IsDisabled { get; set; }

  public CachedHostname(string hostname, DateTime expires, bool isUpcomingExpire, bool isDisabled) {
    Hostname = hostname;
    Expires = expires;
    IsUpcomingExpire = isUpcomingExpire;
    IsDisabled = isDisabled;
  }
}
