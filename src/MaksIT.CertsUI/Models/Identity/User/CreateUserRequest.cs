using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.User;

public class CreateUserRequest : RequestModelBase {
  public required string Username { get; set; }
  public required string Email { get; set; }
  public required string MobileNumber { get; set; }
  public required string Password { get; set; }
  public bool IsGlobalAdmin { get; set; }
  public List<CreateUserEntityScopeRequest>? EntityScopes { get; set; }
}
