using MaksIT.Core.Abstractions.Domain;
using MaksIT.CertsUI.Engine.Facades;

namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// API key aggregate root: key value, optional description and expiry.
/// Authorization (global admin + entity scopes) lives in <see cref="ApiKeyAuthorization"/> (symmetric with User/UserAuthorization).
/// <para>
/// <b>Used by:</b>
/// <list type="bullet">
///   <item>IApiKeyDomainService — ReadAPIKey, WriteAPIKeyAsync; persistence and API key services</item>
///   <item>VaultAuthorizationFilter — validates key then loads ApiKeyAuthorization for request auth</item>
///   <item>APIKeyService — create/patch/delete API keys</item>
/// </list>
/// </para>
/// </summary>
public class ApiKey(
  Guid id,
  string apiKey,
  DateTime createdAt
) : DomainDocumentBase<Guid>(id) {

  /// <summary>
  /// Encrypted API key value.
  /// </summary>
  public string Value { get; private set; } = apiKey;

  /// <summary>
  /// Optional description for the API key.
  /// </summary>
  public string? Description { get; private set; }

  /// <summary>
  /// The date and time when the API key was created.
  /// </summary>
  public DateTime CreatedAt { get; private set; } = createdAt;

  /// <summary>
  /// Optional expiration date for the API key.
  /// </summary>
  public DateTime? ExpiresAt { get; private set; }

  #region New entity constructor
  /// <summary>
  /// Constructs a new APIKey with the specified apiKey value and current UTC time for CreatedAt.
  /// </summary>
  public ApiKey(string apiKey) : this(CombGui.GenerateCombGuid(), apiKey, DateTime.UtcNow) { }
  #endregion

  #region Fluent API for setting properties
  public ApiKey SetApiKey(string apiKey) {
    Value = apiKey;
    return this;
  }

  public ApiKey SetDescription(string? description) {
    Description = description;
    return this;
  }

  public ApiKey SetExpiresAt(DateTime? expiresAt) {
    ExpiresAt = expiresAt;
    return this;
  }
  #endregion
}
