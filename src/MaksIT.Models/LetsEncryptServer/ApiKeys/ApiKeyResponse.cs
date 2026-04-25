using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys;

/// <summary>
/// API key metadata; <see cref="ApiKey"/> is only set when the key is created (Vault-style).
/// </summary>
public class ApiKeyResponse : ResponseModelBase {
  public Guid Id { get; set; }
  /// <summary>Plaintext key; only returned on create.</summary>
  public string ApiKey { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public string? Description { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }
}
