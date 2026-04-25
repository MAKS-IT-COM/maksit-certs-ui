using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys.Search;

public class SearchAPIKeyResponse : ResponseModelBase {
  public Guid Id { get; set; }
  public DateTime CreatedAt { get; set; }
  public string? Description { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public DateTime? RevokedAt { get; set; }
}
