using MaksIT.Core.Abstractions.Webapi;


namespace Models.LetsEncryptServer.Identity.Login;

public class LoginResponse : ResponseModelBase {

  public required string TokenType { get; set; }
  public required string Token { get; set; }
  public required DateTime ExpiresAt { get; set; }
  public required string RefreshToken { get; set; }
  public required DateTime RefreshTokenExpiresAt { get; set; }
}