using MaksIT.Core.Abstractions.Domain;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// API key aggregate: at-rest hash plus optional per-key salt. New keys use <see cref="MaksIT.Core.Security.PasswordHasher"/> with the same app pepper as user passwords.
/// Wire format for new keys is <c>{keyId}|{secret}</c> so validation can load the row and verify salt + hash + pepper.
/// Legacy rows have empty <see cref="KeySalt"/> and are matched only by SHA-256 hex of the opaque secret (no pepper).
/// </summary>
public class ApiKey : DomainDocumentBase<Guid> {

  /// <summary>Per-key salt (empty for legacy keys).</summary>
  public string KeySalt { get; private set; } = string.Empty;

  /// <summary>Hash at rest (PasswordHasher output for new keys; legacy: SHA-256 hex of UTF-8 secret).</summary>
  public string KeyHashHex { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public DateTime CreatedAt { get; private set; }

  public DateTime? ExpiresAt { get; private set; }

  public DateTime? RevokedAtUtc { get; private set; }

  /// <summary>Hydration from persistence.</summary>
  public ApiKey(Guid id, string keySalt, string keyHashHex, DateTime createdAtUtc) : base(id) {
    KeySalt = keySalt ?? string.Empty;
    KeyHashHex = keyHashHex;
    CreatedAt = createdAtUtc;
  }

  public ApiKey SetDescription(string? description) {
    Description = description;
    return this;
  }

  public ApiKey SetExpiresAt(DateTime? expiresAt) {
    ExpiresAt = expiresAt;
    return this;
  }

  public ApiKey SetRevokedAtUtc(DateTime? revokedAtUtc) {
    RevokedAtUtc = revokedAtUtc;
    return this;
  }

  public ApiKey SetKeySalt(string keySalt) {
    KeySalt = keySalt ?? string.Empty;
    return this;
  }

  public ApiKey SetKeyHashHex(string keyHashHex) {
    KeyHashHex = keyHashHex;
    return this;
  }

  public ApiKey Revoke(DateTime utcNow) {
    RevokedAtUtc = utcNow;
    return this;
  }
}
