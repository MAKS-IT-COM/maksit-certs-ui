using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.LetsEncrypt.Models.Interfaces;

namespace MaksIT.LetsEncrypt.Models.Responses {

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
}
