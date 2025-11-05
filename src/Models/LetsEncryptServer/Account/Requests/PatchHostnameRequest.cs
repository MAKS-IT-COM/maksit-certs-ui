using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Account.Requests;
public class PatchHostnameRequest : PatchRequestModelBase {
  public string? Hostname { get; set; }

  public bool? IsDisabled { get; set; }
}

