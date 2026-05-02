using MaksIT.Core.Abstractions.Dto;


namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class UserDto : DtoDocumentBase<Guid> {
  public required string Username { get; set; }
  public string? Email { get; set; }
  public string? MobileNumber { get; set; }
  public bool IsActive { get; set; }
  public bool IsGlobalAdmin { get; set; }

  public required string PasswordSalt { get; set; }
  public required string PasswordHash { get; set; }

  public string? TwoFactorSharedKey { get; set; }

  public DateTime CreatedAt { get; set; }
  public DateTime? LastLogin { get; set; }

  public List<UserEntityScopeDto> EntityScopes { get; set; } = [];
  public List<TwoFactorRecoveryCodeDto> TwoFactorRecoveryCodes { get; set; } = [];
  public List<JwtTokenDto> JwtTokens { get; set; } = [];
}
