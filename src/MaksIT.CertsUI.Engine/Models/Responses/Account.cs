using MaksIT.Core.Security.JWK;
using MaksIT.LetsEncrypt.Entities.Jws;
using MaksIT.LetsEncrypt.Models.Interfaces;

/*
* https://tools.ietf.org/html/draft-ietf-acme-acme-18#section-7.3
*/

namespace MaksIT.LetsEncrypt.Models.Responses;

public class Account : IHasLocation {

  public bool TermsOfServiceAgreed { get; set; }

  /*
  onlyReturnExisting (optional, boolean):  If this field is present
  with the value "true", then the server MUST NOT create a new
  account if one does not already exist.  This allows a client to
  look up an account URL based on an account key
  */
  public bool OnlyReturnExisting { get; set; }

  public string[]? Contacts { get; set; }

  public string? Status { get; set; }

  public string? Id { get; set; }

  public DateTime CreatedAt { get; set; }

  public Jwk? Key { get; set; }

  public string? InitialIp { get; set; }

  public Uri? Orders { get; set; }

  public Uri? Location { get; set; }
}
