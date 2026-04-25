using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.Identity.User.Search;

/// <summary>One user entity scope row in search results (includes parent user name).</summary>
public class SearchUserEntityScopeResponse : ResponseModelBase {
  public required Guid Id { get; set; }
  public required Guid UserId { get; set; }
  public string? Username { get; set; }
  public required Guid EntityId { get; set; }
  public string? EntityName { get; set; }
  public int EntityType { get; set; }
  public int Scope { get; set; }
}
