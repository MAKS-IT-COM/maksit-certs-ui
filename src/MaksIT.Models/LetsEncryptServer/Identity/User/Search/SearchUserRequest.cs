using MaksIT.Models.LetsEncryptServer.Common;

namespace MaksIT.Models.LetsEncryptServer.Identity.User.Search;

public class SearchUserRequest : PagedRequest {
  public string? UsernameFilter { get; set; }
}
