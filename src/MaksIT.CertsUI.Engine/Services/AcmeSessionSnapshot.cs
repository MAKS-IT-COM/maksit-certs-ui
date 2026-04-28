using MaksIT.Core.Security.JWK;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;
using Newtonsoft.Json;

namespace MaksIT.CertsUI.Engine.Services;

/// <summary>JSON-serializable projection of <see cref="MaksIT.CertsUI.Engine.Domain.LetsEncrypt.State"/> for <c>acme_sessions.payload_json</c>.</summary>
internal sealed class AcmeSessionSnapshot {
  public bool IsStaging { get; set; }
  public AcmeDirectory? Directory { get; set; }
  public Order? CurrentOrder { get; set; }
  public List<AuthorizationChallengeChallenge> Challenges { get; set; } = [];
  public RegistrationCache? Cache { get; set; }
  public Jwk? Jwk { get; set; }
  /// <summary>RSA account key as CSP blob when present (same encoding as <see cref="RegistrationCache.AccountKey"/>).</summary>
  public byte[]? AccountKeyCspBlob { get; set; }
}
