using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MaksIT.Core.Security.JWK;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;
using MaksIT.CertsUI.Engine.Domain.Certs;

namespace MaksIT.CertsUI.Engine.Domain.LetsEncrypt;

public class State {
  public bool IsStaging { get; set; }
  public AcmeDirectory? Directory { get; set; }
  public Order? CurrentOrder { get; set; }
  public List<AuthorizationChallengeChallenge> Challenges { get; } = new List<AuthorizationChallengeChallenge>();
  public RegistrationCache? Cache { get; set; }
  public Jwk? Jwk { get; set; }
  public RSA? Rsa { get; set; }

  /// <summary>Returns the session account key pair when both RSA and JWK are present (after <c>Init</c>).</summary>
  public bool TryGetAccountKey([NotNullWhen(true)] out RSA? rsa, [NotNullWhen(true)] out Jwk? jwk) {
    rsa = Rsa;
    jwk = Jwk;
    return rsa is not null && jwk is not null;
  }
}
