namespace MaksIT.CertsUI.Authorization;

/// <summary>
/// API key data from the request.
/// </summary>
public class ApiKeyData {
  public Guid ApiKeyId { get; set; }
  public bool IsGlobalAdmin { get; set; }
  public List<IdentityScopeData>? EntityScopes { get; set; }
}
