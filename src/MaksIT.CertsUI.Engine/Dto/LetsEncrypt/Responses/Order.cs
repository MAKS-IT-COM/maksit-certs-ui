using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Interfaces;

namespace MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

public class OrderIdentifier {
  public string? Type { get; set; }

  public string? Value { get; set; }

}

public class Order : IHasLocation {
  public Uri? Location { get; set; }

  public string? Status { get; set; }

  public DateTime? Expires { get; set; }

  public OrderIdentifier[]? Identifiers { get; set; }

  public DateTime? NotBefore { get; set; }

  public DateTime? NotAfter { get; set; }

  public Problem? Error { get; set; }

  public Uri[]? Authorizations { get; set; }

  public Uri? Finalize { get; set; }

  public Uri? Certificate { get; set; }
}
