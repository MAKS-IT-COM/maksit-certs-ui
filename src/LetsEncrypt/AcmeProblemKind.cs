using MaksIT.Core.Abstractions;

namespace MaksIT.LetsEncrypt;

/// <summary>
/// ACME problem <c>type</c> URIs (RFC 8555 §6.7). <see cref="Unknown"/> covers missing or unrecognized URIs.
/// </summary>
public sealed class AcmeProblemKind : Enumeration {

  public static readonly AcmeProblemKind Unknown = new(0, "");

  public static readonly AcmeProblemKind BadCsr = new(1, "urn:ietf:params:acme:error:badCSR");
  public static readonly AcmeProblemKind BadNonce = new(2, "urn:ietf:params:acme:error:badNonce");
  public static readonly AcmeProblemKind BadPublicKey = new(3, "urn:ietf:params:acme:error:badPublicKey");
  public static readonly AcmeProblemKind BadRevocationReason = new(4, "urn:ietf:params:acme:error:badRevocationReason");
  public static readonly AcmeProblemKind BadSignatureAlgorithm = new(5, "urn:ietf:params:acme:error:badSignatureAlgorithm");
  public static readonly AcmeProblemKind Caa = new(6, "urn:ietf:params:acme:error:caa");
  public static readonly AcmeProblemKind Compound = new(7, "urn:ietf:params:acme:error:compound");
  public static readonly AcmeProblemKind Connection = new(8, "urn:ietf:params:acme:error:connection");
  public static readonly AcmeProblemKind Dns = new(9, "urn:ietf:params:acme:error:dns");
  public static readonly AcmeProblemKind ExternalAccountRequired = new(10, "urn:ietf:params:acme:error:externalAccountRequired");
  public static readonly AcmeProblemKind IncorrectResponse = new(11, "urn:ietf:params:acme:error:incorrectResponse");
  public static readonly AcmeProblemKind InvalidContact = new(12, "urn:ietf:params:acme:error:invalidContact");
  public static readonly AcmeProblemKind Malformed = new(13, "urn:ietf:params:acme:error:malformed");
  public static readonly AcmeProblemKind OrderNotReady = new(14, "urn:ietf:params:acme:error:orderNotReady");
  public static readonly AcmeProblemKind RateLimited = new(15, "urn:ietf:params:acme:error:rateLimited");
  public static readonly AcmeProblemKind RejectedIdentifier = new(16, "urn:ietf:params:acme:error:rejectedIdentifier");
  public static readonly AcmeProblemKind ServerInternal = new(17, "urn:ietf:params:acme:error:serverInternal");
  public static readonly AcmeProblemKind Tls = new(18, "urn:ietf:params:acme:error:tls");
  public static readonly AcmeProblemKind UnsupportedContact = new(19, "urn:ietf:params:acme:error:unsupportedContact");
  public static readonly AcmeProblemKind UnsupportedIdentifier = new(20, "urn:ietf:params:acme:error:unsupportedIdentifier");
  public static readonly AcmeProblemKind UserActionRequired = new(21, "urn:ietf:params:acme:error:userActionRequired");

  private AcmeProblemKind(int id, string name) : base(id, name) { }

  /// <summary>
  /// Resolves a problem <c>type</c> URI from the CA. Comparison is ordinal (RFC 3986).
  /// </summary>
  public static AcmeProblemKind FromTypeUri(string? typeUri) {
    if (string.IsNullOrEmpty(typeUri))
      return Unknown;

    foreach (var kind in GetAll<AcmeProblemKind>()) {
      if (kind == Unknown)
        continue;
      if (kind.Name == typeUri)
        return kind;
    }

    return Unknown;
  }
}
