using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Account.Requests;

public class PatchAccountRequest : PatchRequestModelBase {

  public string Description { get; set; }

  public bool? IsDisabled { get; set; }

  public List<string>? Contacts { get; set; }

  public List<PatchHostnameRequest>? Hostnames { get; set; }
}
