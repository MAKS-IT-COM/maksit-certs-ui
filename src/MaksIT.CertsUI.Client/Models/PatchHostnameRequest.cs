namespace MaksIT.CertsUI.Client.Models;

public class PatchHostnameRequest {
  public Dictionary<string, int>? Operations { get; set; }
  public string? Hostname { get; set; }
  public bool? IsDisabled { get; set; }
}
