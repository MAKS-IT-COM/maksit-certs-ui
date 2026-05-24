namespace MaksIT.CertsUI.Client.Models;

public class PatchAccountRequest {
  /// <summary>Per-field patch operations (keys are camelCase to match the API).</summary>
  public Dictionary<string, int>? Operations { get; set; }

  public string? Description { get; set; }
  public bool? IsDisabled { get; set; }
  public List<string>? Contacts { get; set; }
  public List<PatchHostnameRequest>? Hostnames { get; set; }
}
