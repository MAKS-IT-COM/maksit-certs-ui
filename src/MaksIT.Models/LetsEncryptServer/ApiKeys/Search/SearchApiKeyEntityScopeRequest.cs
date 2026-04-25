using MaksIT.Models.LetsEncryptServer.Common;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys.Search;

/// <summary>Paged search for API key entity scopes (Vault-shaped).</summary>
public class SearchApiKeyEntityScopeRequest : PagedRequest {
  /// <summary>Optional filter by parent API key ID.</summary>
  public Guid? ApiKeyId { get; set; }
}
