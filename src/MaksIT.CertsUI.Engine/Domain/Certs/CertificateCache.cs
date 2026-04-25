using MaksIT.Core.Abstractions.Domain;
using MaksIT.CertsUI.Engine.Facades;
using Newtonsoft.Json;

namespace MaksIT.CertsUI.Engine.Domain.Certs;

/// <summary>
/// Per-hostname certificate material (PEM and optional private key blobs) owned by <see cref="RegistrationCache"/>.
/// </summary>
/// <param name="id">Document id (stable when rehydrating from JSON).</param>
/// <param name="cert">Certificate PEM.</param>
/// <param name="privateKey">Optional CSP blob for the private key.</param>
/// <param name="privatePem">Optional PEM for the private key.</param>
/// <param name="isDisabled">When true, this host entry is ignored for deployment.</param>
[JsonObject(MemberSerialization.OptIn)]
public class CertificateCache(
  Guid id,
  string cert,
  byte[]? privateKey,
  string? privatePem,
  bool isDisabled
) : DomainDocumentBase<Guid>(id) {

  #region Master data Properties

  /// <summary>
  /// Certificate chain / PEM text from ACME.
  /// </summary>
  [JsonProperty(Order = 1)]
  public string Cert { get; set; } = cert;

  /// <summary>
  /// Optional Windows CSP blob for the private key material.
  /// </summary>
  [JsonProperty(Order = 2)]
  public byte[]? Private { get; set; } = privateKey;

  /// <summary>
  /// Optional RSA private key in PEM form.
  /// </summary>
  [JsonProperty(Order = 3)]
  public string? PrivatePem { get; set; } = privatePem;

  /// <summary>
  /// When true, this cached entry is treated as disabled.
  /// </summary>
  [JsonProperty(Order = 4)]
  public bool IsDisabled { get; set; } = isDisabled;

  #endregion

  #region New entity constructor

  /// <summary>
  /// Parameterless constructor for JSON deserialization; assigns a new CombGuid <see cref="DomainDocumentBase{Guid}.Id"/>.
  /// </summary>
  public CertificateCache() : this(CombGui.GenerateCombGuid(), string.Empty, null, null, false) { }

  /// <summary>
  /// Creates a new cached certificate row with a generated id.
  /// </summary>
  public CertificateCache(string cert, byte[]? privateKey, string? privatePem, bool isDisabled)
    : this(CombGui.GenerateCombGuid(), cert, privateKey, privatePem, isDisabled) { }

  #endregion

  #region Fluent API for setting properties

  /// <summary>
  /// Sets the certificate PEM text.
  /// </summary>
  public CertificateCache SetCert(string cert) {
    Cert = cert;
    return this;
  }

  /// <summary>
  /// Sets the optional CSP blob for the private key.
  /// </summary>
  public CertificateCache SetPrivate(byte[]? privateKey) {
    Private = privateKey;
    return this;
  }

  /// <summary>
  /// Sets the optional PEM for the private key.
  /// </summary>
  public CertificateCache SetPrivatePem(string? privatePem) {
    PrivatePem = privatePem;
    return this;
  }

  /// <summary>
  /// Sets whether this cached certificate entry is disabled.
  /// </summary>
  public CertificateCache SetIsDisabled(bool isDisabled) {
    IsDisabled = isDisabled;
    return this;
  }

  #endregion
}
