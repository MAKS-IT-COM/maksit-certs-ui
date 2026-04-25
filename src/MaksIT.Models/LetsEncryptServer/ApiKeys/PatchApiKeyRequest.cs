using MaksIT.Core.Abstractions.Webapi;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys;

public class PatchApiKeyRequest : PatchRequestModelBase {
  public string? Description { get; set; }
  public DateTime? ExpiresAt { get; set; }
}
