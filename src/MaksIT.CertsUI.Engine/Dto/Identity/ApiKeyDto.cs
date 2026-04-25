using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Identity;

/// <summary>
/// PostgreSQL <c>api_keys</c> row (Linq2DB).
/// </summary>
public class ApiKeyDto : DtoDocumentBase<Guid> {
  public string? Description { get; set; }
  public DateTime? ExpiresAtUtc { get; set; }

  /// <summary>Empty for legacy rows (SHA-256-only); otherwise per-key salt for <see cref="MaksIT.Core.Security.PasswordHasher"/>.</summary>
  public string KeySalt { get; set; } = string.Empty;

  /// <summary>Hash at rest (PasswordHasher digest or legacy SHA-256 hex).</summary>
  public required string KeyHashHex { get; set; }

  public DateTime CreatedAtUtc { get; set; }
  public DateTime? RevokedAtUtc { get; set; }
}
