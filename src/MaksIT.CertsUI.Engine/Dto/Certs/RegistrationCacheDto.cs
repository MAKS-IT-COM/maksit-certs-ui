using MaksIT.Core.Abstractions.Dto;

namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>
/// PostgreSQL <c>registration_caches</c> row: ACME registration payload as JSON text.
/// </summary>
public class RegistrationCacheDto : DtoDocumentBase<Guid> {
  /// <summary>
  /// Backward-compatible alias for <see cref="DtoDocumentBase{Guid}.Id"/>.
  /// </summary>
  public Guid AccountId {
    get => Id;
    set => Id = value;
  }

  public long Version { get; set; }
  public required string PayloadJson { get; set; }
}
