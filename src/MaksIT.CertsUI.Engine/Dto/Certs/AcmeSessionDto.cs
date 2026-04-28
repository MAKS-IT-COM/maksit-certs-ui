namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>PostgreSQL <c>acme_sessions</c>: shared ACME flow state keyed by browser session id (survives HA / any replica).</summary>
public sealed class AcmeSessionDto {
  public Guid SessionId { get; set; }
  public string PayloadJson { get; set; } = "{}";
  public DateTimeOffset UpdatedAtUtc { get; set; }
  public DateTimeOffset ExpiresAtUtc { get; set; }
}
