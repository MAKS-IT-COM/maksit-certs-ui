using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

/// <summary>
/// PostgreSQL <c>users</c> row (Linq2DB). JWT sessions live in <c>jwt_tokens</c>; <see cref="JwtTokens"/> is not a mapped column.
/// </summary>
public class UserDto : DtoDocumentBase<Guid> {
  public required string Name { get; set; }
  public required string Salt { get; set; }
  public required string Hash { get; set; }
  public DateTime LastLoginUtc { get; set; }
  public bool IsActive { get; set; } = true;
  public string? TwoFactorSharedKey { get; set; }

  /// <summary>Child rows in <c>jwt_tokens</c>; not mapped as a column (loaded by persistence).</summary>
  public List<JwtTokenDto> JwtTokens { get; set; } = [];

  /// <summary>Child rows in <c>two_factor_recovery_codes</c>; not mapped as a column (loaded by persistence).</summary>
  public List<TwoFactorRecoveryCodeDto> TwoFactorRecoveryCodes { get; set; } = [];
}
