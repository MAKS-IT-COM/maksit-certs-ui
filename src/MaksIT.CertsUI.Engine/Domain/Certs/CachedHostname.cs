using MaksIT.Core.Abstractions.Domain;
using MaksIT.CertsUI.Engine.Facades;
using Newtonsoft.Json;

namespace MaksIT.CertsUI.Engine.Domain.Certs;

/// <summary>
/// Constructs a <see cref="CachedHostname"/> row for list views (derived from cached certificate expiry).
/// </summary>
/// <param name="id">Document id.</param>
/// <param name="hostname">DNS name.</param>
/// <param name="expires">Certificate <c>NotAfter</c> (UTC).</param>
/// <param name="isUpcomingExpire">True when expiry is within the configured warning window.</param>
/// <param name="isDisabled">Mirrors <see cref="CertificateCache.IsDisabled"/> for the host.</param>
[JsonObject(MemberSerialization.OptIn)]
public class CachedHostname(
  Guid id,
  string hostname,
  DateTime expires,
  bool isUpcomingExpire,
  bool isDisabled
) : DomainDocumentBase<Guid>(id) {

  #region Master data Properties

  /// <summary>
  /// DNS hostname for the cached certificate.
  /// </summary>
  [JsonProperty(Order = 1)]
  public string Hostname { get; set; } = hostname;

  /// <summary>
  /// Certificate expiration instant.
  /// </summary>
  [JsonProperty(Order = 2)]
  public DateTime Expires { get; set; } = expires;

  /// <summary>
  /// True when the certificate expires within the upcoming-renewal window.
  /// </summary>
  [JsonProperty(Order = 3)]
  public bool IsUpcomingExpire { get; set; } = isUpcomingExpire;

  /// <summary>
  /// True when the underlying cached entry is disabled.
  /// </summary>
  [JsonProperty(Order = 4)]
  public bool IsDisabled { get; set; } = isDisabled;

  #endregion

  #region New entity constructor

  /// <summary>
  /// Parameterless constructor for JSON deserialization.
  /// </summary>
  public CachedHostname() : this(Guid.Empty, string.Empty, default, false, false) { }

  /// <summary>
  /// Creates a row with a generated document id (used by <see cref="RegistrationCache.GetHosts"/>).
  /// </summary>
  public CachedHostname(string hostname, DateTime expires, bool isUpcomingExpire, bool isDisabled)
    : this(CombGui.GenerateCombGuid(), hostname, expires, isUpcomingExpire, isDisabled) { }

  #endregion

  #region Fluent API for setting properties

  /// <summary>
  /// Sets the DNS hostname.
  /// </summary>
  public CachedHostname SetHostname(string hostname) {
    Hostname = hostname;
    return this;
  }

  /// <summary>
  /// Sets the expiration instant.
  /// </summary>
  public CachedHostname SetExpires(DateTime expires) {
    Expires = expires;
    return this;
  }

  /// <summary>
  /// Sets whether the certificate is in the upcoming-expiry window.
  /// </summary>
  public CachedHostname SetIsUpcomingExpire(bool isUpcomingExpire) {
    IsUpcomingExpire = isUpcomingExpire;
    return this;
  }

  /// <summary>
  /// Sets whether the host is disabled in cache.
  /// </summary>
  public CachedHostname SetIsDisabled(bool isDisabled) {
    IsDisabled = isDisabled;
    return this;
  }

  #endregion
}
