using MaksIT.Core.Security.JWK;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Services;
using System.Security.Cryptography;


namespace MaksIT.LetsEncrypt.Entities.LetsEncrypt;

public class State {
  public bool IsStaging { get; set; }
  public AcmeDirectory? Directory { get; set; }
  public Order? CurrentOrder { get; set; }
  public List<AuthorizationChallengeChallenge> Challenges { get; } = new List<AuthorizationChallengeChallenge>();
  public RegistrationCache? Cache { get; set; }
  public Jwk? Jwk { get; set; }
  public RSA? Rsa { get; set; }
}
