

namespace MaksIT.Models.LetsEncryptServer.Account.Requests;
public class PatchHostnameRequest {
  public PatchAction<string>? Hostname { get; set; }

  public PatchAction<bool>? IsDisabled { get; set; }
}

