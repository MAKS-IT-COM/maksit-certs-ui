using MaksIT.Models.LetsEncryptServer.Common;

namespace MaksIT.Models.LetsEncryptServer.ApiKeys.Search;

public class SearchAPIKeyRequest : PagedRequest {
  public string? DescriptionFilter { get; set; }
}
