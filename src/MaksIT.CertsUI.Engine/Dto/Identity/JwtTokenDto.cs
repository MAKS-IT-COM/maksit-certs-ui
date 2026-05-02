using MaksIT.Core.Abstractions.Dto;


namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class JwtTokenDto : DtoDocumentBase<Guid> {
  public required string Token { get; set; }
  public DateTime IssuedAt { get; set; }
  public DateTime ExpiresAt { get; set; }
  public required string RefreshToken { get; set; }
  public DateTime RefreshTokenExpiresAt { get; set; }
  public bool IsRevoked { get; set; } = false;

  public Guid UserId { get; set; }
}
