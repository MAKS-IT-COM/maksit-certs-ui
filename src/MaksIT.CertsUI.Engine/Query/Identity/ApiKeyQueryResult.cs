using MaksIT.Core.Abstractions.Query;

namespace MaksIT.CertsUI.Engine.Query.Identity;

/// <summary>
/// Projection for API key list/search rows.
/// </summary>
public class ApiKeyQueryResult : QueryResultBase<Guid> {
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }
}
