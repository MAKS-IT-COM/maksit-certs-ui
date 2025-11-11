using MaksIT.Core.Abstractions;

namespace MaksIT.Webapi.Authorization;

public class HttpContextValue : Enumeration {
  public static readonly HttpContextValue JwtTokenData = new(0, "JwtTokenData");

  private HttpContextValue(int id, string value) : base(id, value) { }
}
