using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

/// <summary>Row in <c>two_factor_recovery_codes</c>.</summary>
public class TwoFactorRecoveryCodeDto : DtoDocumentBase<Guid> {
  public Guid UserId { get; set; }
  public required string Salt { get; set; }
  public required string Hash { get; set; }
  public bool IsUsed { get; set; }
}
