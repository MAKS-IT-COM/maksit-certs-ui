namespace MaksIT.CertsUI.Authorization;

public class JwtTokenData {
  public Guid UserId { get; set; }
  public required string Username { get; set; }
  public required string Token { get; set; }
  public required DateTime IssuedAt { get; set; }
  public required DateTime ExpiresAt { get; set; }
}