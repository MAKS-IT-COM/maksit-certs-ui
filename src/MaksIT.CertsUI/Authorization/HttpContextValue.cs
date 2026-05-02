using MaksIT.Core.Abstractions;

namespace MaksIT.CertsUI.Authorization;

public class HttpContextValue : Enumeration {
  public static readonly HttpContextValue JwtTokenData = new(0, "JwtTokenData");
  public static readonly HttpContextValue CertsUIAuthorizationData = new(1, "CertsUIAuthorizationData");

  private HttpContextValue(int id, string value) : base(id, value) { }
}
