using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Identity.Login;

public class LoginResponse : ResponseModelBase {
  public required string TokenType { get; set; }
  public required string Token { get; set; }
  public required DateTime ExpiresAt { get; set; }
  public required string RefreshToken { get; set; }
  public required DateTime RefreshTokenExpiresAt { get; set; }

  /// <summary>Actual login username; use this for display so it is not replaced by a display name (e.g. "Organization Admin") from the JWT name claim.</summary>
  public string? Username { get; set; }
}