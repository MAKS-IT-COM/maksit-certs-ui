namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>
/// PostgreSQL <c>acme_sessions</c>: shared ACME flow state keyed by browser session id (survives HA / any replica).
/// <see cref="AccountScopeId"/> ties the row to <see cref="Domain.Certs.RegistrationCache.AccountId"/> when the interactive flow has loaded that aggregate—sibling browser sessions for the same account are removed on save (JWT-style pruning on the registration aggregate scope).
/// </summary>
public sealed class AcmeSessionDto {
  public Guid SessionId { get; set; }

  /// <summary>
  /// When <see cref="Domain.LetsEncrypt.State.Cache"/> is set, equals <see cref="Domain.Certs.RegistrationCache.AccountId"/>; otherwise <c>null</c> (configure phase before account is bound).
  /// </summary>
  public Guid? AccountScopeId { get; set; }

  public string PayloadJson { get; set; } = "{}";
  public DateTimeOffset UpdatedAtUtc { get; set; }
  public DateTimeOffset ExpiresAtUtc { get; set; }
}
