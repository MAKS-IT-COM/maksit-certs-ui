using MaksIT.Core.Abstractions.Dto;


namespace MaksIT.CertsUI.Engine.Dto.Identity;

public class TwoFactorRecoveryCodeDto : DtoDocumentBase<Guid> {
  public required string Salt { get; set; }
  public required string Hash { get; set; }
  public bool IsUsed { get; set; } = false;

  public Guid UserId { get; set; }
}
