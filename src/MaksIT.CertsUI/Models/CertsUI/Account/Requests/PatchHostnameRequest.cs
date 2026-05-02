using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.Account.Requests;

public class PatchHostnameRequest : PatchRequestModelBase {
  public string? Hostname { get; set; }
  public bool? IsDisabled { get; set; }
}
