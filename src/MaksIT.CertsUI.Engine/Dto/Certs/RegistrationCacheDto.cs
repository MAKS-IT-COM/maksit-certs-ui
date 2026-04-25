namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>
/// PostgreSQL <c>registration_caches</c> row: ACME registration payload as JSON text.
/// </summary>
public class RegistrationCacheDto {
  public Guid AccountId { get; set; }
  public long Version { get; set; }
  public required string PayloadJson { get; set; }
}
