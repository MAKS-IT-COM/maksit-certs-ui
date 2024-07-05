

namespace MaksIT.Models.LetsEncryptServer.Account.Requests;

public class PatchAccountRequest {

  public PatchAction<string>? Description { get; set; }

  public PatchAction<bool>? IsDisabled { get; set; }

  public List<PatchAction<string>>? Contacts { get; set; }

  public List<PatchHostnameRequest>? Hostnames { get; set; }
}
