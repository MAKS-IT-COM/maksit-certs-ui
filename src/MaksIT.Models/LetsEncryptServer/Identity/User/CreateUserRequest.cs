using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.Identity.User;

public class CreateUserRequest : RequestModelBase {
  public required string Username { get; set; }
  public required string Password { get; set; }
}
