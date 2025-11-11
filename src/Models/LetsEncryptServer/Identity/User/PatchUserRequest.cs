using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Identity.User;

public class PatchUserRequest : PatchRequestModelBase {
  public string? Password { get; set; }
}
