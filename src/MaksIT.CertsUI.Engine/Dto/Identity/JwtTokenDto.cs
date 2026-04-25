using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

/// <summary>
/// Row in <c>jwt_tokens</c> (FK to <c>users</c>).
/// </summary>
public class JwtTokenDto : DtoDocumentBase<Guid> {
  public required string Token { get; set; }
  public DateTime IssuedAt { get; set; }
  public DateTime ExpiresAt { get; set; }
  public required string RefreshToken { get; set; }
  public DateTime RefreshTokenExpiresAt { get; set; }
  public bool IsRevoked { get; set; }
  public Guid UserId { get; set; }
}
