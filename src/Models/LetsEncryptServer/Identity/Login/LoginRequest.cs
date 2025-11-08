using MaksIT.Core.Abstractions.Webapi;


namespace Models.LetsEncryptServer.Identity.Login;

public class LoginRequest : RequestModelBase {
  public required string Username { get; set; }
  public required string Password { get; set; }
  public string? TwoFactorCode { get; set; }
  public string? TwoFactorRecoveryCode { get; set; }
}
