using System.Security.Cryptography;
using MaksIT.CertsUI.Engine.Domain.LetsEncrypt;
using Newtonsoft.Json;

namespace MaksIT.CertsUI.Engine.Services;

internal static class AcmeSessionJsonSerializer {
  private static readonly JsonSerializerSettings Settings = new() {
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.None
  };

  public static string ToJson(State state) {
    var snap = new AcmeSessionSnapshot {
      IsStaging = state.IsStaging,
      Directory = state.Directory,
      CurrentOrder = state.CurrentOrder,
      Challenges = [.. state.Challenges],
      Cache = state.Cache,
      Jwk = state.Jwk,
      AccountKeyCspBlob = state.Rsa is RSACryptoServiceProvider csp ? csp.ExportCspBlob(true) : null
    };
    return JsonConvert.SerializeObject(snap, Settings);
  }

  public static State FromJson(string json) {
    if (string.IsNullOrWhiteSpace(json))
      return new State();
    var snap = JsonConvert.DeserializeObject<AcmeSessionSnapshot>(json, Settings);
    if (snap == null)
      return new State();
    var state = new State {
      IsStaging = snap.IsStaging,
      Directory = snap.Directory,
      CurrentOrder = snap.CurrentOrder,
      Cache = snap.Cache,
      Jwk = snap.Jwk
    };
    foreach (var c in snap.Challenges) {
      if (c != null)
        state.Challenges.Add(c);
    }
    if (snap.AccountKeyCspBlob is { Length: > 0 }) {
      var rsa = new RSACryptoServiceProvider();
      rsa.ImportCspBlob(snap.AccountKeyCspBlob);
      state.Rsa = rsa;
    }
    return state;
  }
}
