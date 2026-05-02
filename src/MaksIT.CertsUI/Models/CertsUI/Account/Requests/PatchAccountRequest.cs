using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.Account.Requests;

public class PatchAccountRequest : PatchRequestModelBase {
  public string? Description { get; set; }
  public bool? IsDisabled { get; set; }
  public List<string>? Contacts { get; set; }
  public List<PatchHostnameRequest>? Hostnames { get; set; }
}
