using MaksIT.Core.Abstractions.Webapi;
using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Models.APIKeys;

public class ApiKeyResponse : ResponseModelBase {
  public Guid Id { get; set; }
  public required string ApiKey { get; set; }
  public DateTime CreatedAt { get; set; }
  public string? Description { get; set; } // Optional description for the API key
  public DateTime? ExpiresAt { get; set; }
  public bool IsGlobalAdmin { get; set; }
  public List<ApiKeyEntityScopeRsponse>? EntityScopes { get; set; }
}
