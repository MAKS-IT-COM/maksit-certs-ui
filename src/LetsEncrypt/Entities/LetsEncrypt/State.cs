using MaksIT.Core.Security.JWK;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.LetsEncrypt.Services;
using System.Security.Cryptography;


namespace MaksIT.LetsEncrypt.Entities.LetsEncrypt;

public class State {
  public bool IsStaging { get; set; }
  public AcmeDirectory? Directory { get; set; }
  public JwsService? JwsService { get; set; }
  public Order? CurrentOrder { get; set; }
  public List<AuthorizationChallengeChallenge> Challenges { get; } = new List<AuthorizationChallengeChallenge>();
  public string? Nonce { get; set; }
  public RegistrationCache? Cache { get; set; }



  public Jwk? Jwk;
  private RSA? RSA;
}
