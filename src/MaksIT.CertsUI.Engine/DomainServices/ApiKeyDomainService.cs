using System.Security.Cryptography;
using System.Text;
using MaksIT.Core.Security;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Facades;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.DomainServices;

public interface IApiKeyDomainService {
  #region Read
  Task<Result<Guid>> TryValidateKeyAsync(string? rawKey, CancellationToken cancellationToken = default);
  Task<Result<ApiKey?>> ReadAPIKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
  #endregion

  #region Write
  Task<Result<(ApiKey ApiKey, string PlainKey)?>> CreateAPIKeyAsync(string? description, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);
  Task<Result<ApiKey?>> WriteAPIKeyAsync(Guid id, string? description, DateTime? expiresAtUtc, bool removeExpiry, CancellationToken cancellationToken = default);
  #endregion

  #region Delete
  Task<Result> DeleteAPIKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
  #endregion
}

public class ApiKeyDomainService(
  ILogger<ApiKeyDomainService> logger,
  IAPIKeyPersistanceService apiKeyPersistence,
  IIdentityDomainConfiguration identityConfiguration
) : IApiKeyDomainService {

  private readonly ILogger<ApiKeyDomainService> _logger = logger;
  private readonly IAPIKeyPersistanceService _apiKeyPersistence = apiKeyPersistence;
  private readonly IIdentityDomainConfiguration _identityConfiguration = identityConfiguration;

  #region Read
  public async Task<Result<Guid>> TryValidateKeyAsync(string? rawKey, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(rawKey))
      return Result<Guid>.Forbidden(default, "API key is missing.");

    var trimmed = rawKey.Trim();
    var pipeIdx = trimmed.IndexOf('|', StringComparison.Ordinal);
    if (pipeIdx > 0 && pipeIdx < trimmed.Length - 1) {
      var idPart = trimmed[..pipeIdx];
      var secretPart = trimmed[(pipeIdx + 1)..];
      if (Guid.TryParse(idPart, out var keyId)) {
        if (string.IsNullOrWhiteSpace(_identityConfiguration.Pepper)) {
          _logger.LogWarning("API key validation failed: pepper is not configured.");
          return Result<Guid>.Forbidden(default, "Invalid API key.");
        }

        var read = await _apiKeyPersistence.ReadByIdAsync(keyId, cancellationToken);
        if (!read.IsSuccess || read.Value == null)
          return Result<Guid>.Forbidden(default, "Invalid API key.");
        var key = read.Value;
        if (string.IsNullOrEmpty(key.KeySalt))
          return Result<Guid>.Forbidden(default, "Invalid API key.");
        if (key.RevokedAtUtc != null)
          return Result<Guid>.Forbidden(default, "Invalid API key.");
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
          return Result<Guid>.Forbidden(default, "API key is expired.");

        if (PasswordHasher.TryValidateHash(secretPart, key.KeySalt, key.KeyHashHex, _identityConfiguration.Pepper, out var isValid, out var errorMessage)) {
          if (isValid)
            return Result<Guid>.Ok(key.Id);
          return Result<Guid>.Forbidden(default, "Invalid API key.");
        }

        _logger.LogWarning("API key validation cryptographic error: {Message}", errorMessage);
        return Result<Guid>.Forbidden(default, "Invalid API key.");
      }
    }

    return await _apiKeyPersistence.TryValidateLegacyKeyHashAsync(LegacySha256Hex(trimmed), cancellationToken);
  }

  public Task<Result<ApiKey?>> ReadAPIKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default) =>
    _apiKeyPersistence.ReadByIdAsync(apiKeyId, cancellationToken);
  #endregion

  #region Write

  /// <summary>
  /// Creates a new API key: generates opaque secret material, stores only a salted hash (with app pepper), and returns the plaintext wire form once.
  /// <para>
  /// <b>Parallels MaksIT.Vault:</b> Vault’s <c>ApiKeyService.CreateAPIKeyAsync</c> pulls random bytes via <c>ITrngClient</c>, persists the key, then maps the domain (which retains the value for the response).
  /// Certs keeps only a hash at rest; the caller must persist the returned plaintext (<c>PlainKey</c> tuple element) client-side — it is not available on later reads (see <see cref="ReadAPIKeyAsync"/>).
  /// </para>
  /// </summary>
  public async Task<Result<(ApiKey ApiKey, string PlainKey)?>> CreateAPIKeyAsync(string? description, DateTime? expiresAtUtc, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(_identityConfiguration.Pepper)) {
      _logger.LogWarning("Cannot create API key: pepper is not configured.");
      return Result<(ApiKey ApiKey, string PlainKey)?>.InternalServerError(null, ["Password pepper is not configured."]);
    }

    // 1) Secret material (Vault: ITrngClient.GetRandomBytesBase64Async; Certs: in-process cryptographic RNG)
    var rawSecret = GenerateOpaqueSecretBase64();

    // 2) Stable id for wire prefix and row primary key
    var id = CombGui.GenerateCombGuid();

    // 3) Salt + hash with shared pepper (never persist rawSecret)
    if (!PasswordHasher.TryCreateSaltedHash(rawSecret, _identityConfiguration.Pepper, out (string PasswordSalt, string Hash)? salted, out string? errorMessage)) {
      _logger.LogError("Failed to hash new API key: {Message}", errorMessage);
      return Result<(ApiKey ApiKey, string PlainKey)?>.InternalServerError(null, [errorMessage ?? "Failed to create API key hash."]);
    }

    var apiKey = new ApiKey(id, salted!.Value.PasswordSalt, salted.Value.Hash, DateTime.UtcNow)
      .SetDescription(description)
      .SetExpiresAt(expiresAtUtc);

    // 4) Persist aggregate
    var write = await _apiKeyPersistence.InsertAsync(apiKey, cancellationToken);
    if (!write.IsSuccess)
      return write.ToResultOfType<(ApiKey ApiKey, string PlainKey)?>(null);

    // 5) One-time wire form for clients: "{keyId:N}|{opaqueSecret}" (validated in TryValidateKeyAsync)
    var plainWire = FormatPlainWire(id, rawSecret);
    return Result<(ApiKey ApiKey, string PlainKey)?>.Ok((apiKey, plainWire));
  }

  /// <summary>32 random bytes, Base64-encoded (same size as Vault’s TRNG create path).</summary>
  private static string GenerateOpaqueSecretBase64() =>
    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

  /// <summary>Wire token: id without braces + pipe + secret (parsed by <see cref="TryValidateKeyAsync"/>).</summary>
  private static string FormatPlainWire(Guid id, string rawSecret) => $"{id:N}|{rawSecret}";

  public async Task<Result<ApiKey?>> WriteAPIKeyAsync(Guid id, string? description, DateTime? expiresAtUtc, bool removeExpiry, CancellationToken cancellationToken = default) {
    var read = await _apiKeyPersistence.ReadByIdAsync(id, cancellationToken);
    if (!read.IsSuccess || read.Value == null)
      return read;

    var apiKey = read.Value.SetDescription(description);
    if (removeExpiry)
      apiKey.SetExpiresAt(null);
    else
      apiKey.SetExpiresAt(expiresAtUtc);

    var write = await _apiKeyPersistence.UpdateAsync(apiKey, cancellationToken);
    if (!write.IsSuccess)
      return write.ToResultOfType<ApiKey?>(null);

    return Result<ApiKey?>.Ok(apiKey);
  }
  #endregion

  #region Delete
  public async Task<Result> DeleteAPIKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default) {
    var read = await _apiKeyPersistence.ReadByIdAsync(apiKeyId, cancellationToken);
    if (!read.IsSuccess)
      return Result.InternalServerError(read.Messages?.ToArray() ?? []);
    if (read.Value == null)
      return Result.NotFound("API key not found.");

    return await _apiKeyPersistence.DeleteByIdAsync(apiKeyId, cancellationToken);
  }
  #endregion

  /// <summary>Legacy storage: SHA-256 hex of UTF-8 opaque secret (no salt, no pepper).</summary>
  private static string LegacySha256Hex(string rawKey) {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
    return Convert.ToHexString(bytes);
  }
}
