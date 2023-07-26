
using MaksIT.LetsEncrypt.Entities;

namespace MaksIT.LetsEncrypt.Models.Responses {
  public class AuthorizationChallengeResponse {
    public OrderIdentifier? Identifier { get; set; }

    public string? Status { get; set; }

    public DateTime? Expires { get; set; }

    public bool Wildcard { get; set; }

    public AuthorizationChallenge[]? Challenges { get; set; }
  }

  public class AuthorizeChallenge {
    public string? KeyAuthorization { get; set; }
  }
}
