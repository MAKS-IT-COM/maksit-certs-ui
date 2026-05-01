using System.Security.Cryptography;
using MaksIT.Core.Extensions;
using MaksIT.Core.Security.JWK;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

namespace MaksIT.CertsUI.Engine.Persistance.Mappers;

/// <summary>
/// Maps ACME browser-session <see cref="State"/> to/from <c>acme_sessions.payload_json</c>.
/// Used by <see cref="MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db.AcmeSessionPersistanceServiceLinq2Db"/>.
/// </summary>
internal static class AcmeSessionPayloadMapper {

  public static string ToPayloadJson(State state) {
    ArgumentNullException.ThrowIfNull(state);
    var snap = new AcmeSessionPayloadSnapshot {
      IsStaging = state.IsStaging,
      Directory = state.Directory,
      CurrentOrder = state.CurrentOrder,
      Challenges = [.. state.Challenges],
      Cache = state.Cache,
      Jwk = state.Jwk,
      AccountKeyCspBlob = state.Rsa is RSACryptoServiceProvider csp ? csp.ExportCspBlob(true) : null
    };
    return snap.ToJson();
  }

  public static State FromPayloadJson(string json) {
    if (string.IsNullOrWhiteSpace(json))
      return new State();

    var snap = json.ToObject<AcmeSessionPayloadSnapshot>();
    if (snap == null)
      return new State();

    var state = new State {
      IsStaging = snap.IsStaging,
      Directory = snap.Directory,
      CurrentOrder = snap.CurrentOrder,
      Cache = snap.Cache,
      Jwk = snap.Jwk
    };

    foreach (var c in snap.Challenges ?? [])
      if (c != null)
        state.Challenges.Add(c);

    if (snap.AccountKeyCspBlob is { Length: > 0 }) {
      var rsa = new RSACryptoServiceProvider();
      rsa.ImportCspBlob(snap.AccountKeyCspBlob);
      state.Rsa = rsa;
    }

    return state;
  }

  /// <summary>DTO shape stored in <c>payload_json</c> (not the in-memory <see cref="State"/>).</summary>
  private sealed class AcmeSessionPayloadSnapshot {
    public bool IsStaging { get; set; }
    public AcmeDirectory? Directory { get; set; }
    public Order? CurrentOrder { get; set; }
    public List<AuthorizationChallengeChallenge> Challenges { get; set; } = [];
    public RegistrationCache? Cache { get; set; }
    public Jwk? Jwk { get; set; }
    /// <summary>RSA account key CSP blob when present (same encoding as <see cref="RegistrationCache.AccountKey"/>).</summary>
    public byte[]? AccountKeyCspBlob { get; set; }
  }
}
