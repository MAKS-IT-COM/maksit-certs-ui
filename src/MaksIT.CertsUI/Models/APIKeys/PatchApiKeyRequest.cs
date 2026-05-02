using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.CertsUI.Models.APIKeys;

public class PatchApiKeyRequest : PatchRequestModelBase {

  public bool? IsGlobalAdmin { get; set; }
  public List<PatchApiKeyEntityScopeRequest>? EntityScopes { get; set; }

  public string? Description { get; set; }

  public DateTime? ExpiresAt { get; set; }
}
