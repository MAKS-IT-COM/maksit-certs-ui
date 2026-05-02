using MaksIT.Core.Abstractions.Webapi;
using MaksIT.CertsUI.Engine;


namespace MaksIT.CertsUI.Models.APIKeys;


public class CreateApiKeyRequest : RequestModelBase {
  public string? Description { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public bool IsGlobalAdmin { get; set; }
  public List<CreateApiKeyEntityScopeRequest>? EntityScopes { get; set; }
}
