using MaksIT.Core.Abstractions.Query;
using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Engine.Query.Identity;

public class ApiKeyQueryResult : QueryResultBase<Guid> {
  public string? Description { get; set; } // Optional description for the API key

  public DateTime CreatedAt { get; set; }
  public DateTime? ExpiresAt { get; set; } // Optional expiration date

  #region Authorization management
  public bool IsGlobalAdmin { get; set; }
  #endregion
}
