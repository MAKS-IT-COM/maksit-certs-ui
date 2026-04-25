using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys.Search;

/// <summary>One API key entity scope row in search results (includes parent API key description).</summary>
public class SearchApiKeyEntityScopeResponse : ResponseModelBase {
  public required Guid Id { get; set; }
  public required Guid ApiKeyId { get; set; }
  public string? Description { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  /// <summary>Numeric discriminator / enum value (product-specific).</summary>
  public int EntityType { get; set; }
  /// <summary>Permission flags as integer (product-specific).</summary>
  public int Scope { get; set; }
}
