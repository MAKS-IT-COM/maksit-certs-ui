using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MaksIT.Core.Abstractions.Domain;
using MaksIT.Core.Security.JWK;
using MaksIT.CertsUI.Engine.Facades;
using Newtonsoft.Json;

namespace MaksIT.CertsUI.Engine.Domain.Certs;

/// <summary>
/// ACME account registration aggregate root: directory account material, issued certificates, and renewal cooldown metadata.
/// <para>
/// <b>Used by:</b>
/// <list type="bullet">
///   <item><see cref="MaksIT.CertsUI.Engine.Services.ILetsEncryptService"/> — session state and cert issuance</item>
///   <item><see cref="MaksIT.CertsUI.Engine.Persistance.Services.IRegistrationCachePersistanceService"/> — JSON payload in PostgreSQL</item>
///   <item><see cref="MaksIT.CertsUI.Engine.DomainServices.ICertsFlowDomainService"/> — full ACME flows</item>
/// </list>
/// </para>
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class RegistrationCache : DomainDocumentBase<Guid> {
  /// <summary>
  /// Persistence row version for optimistic concurrency on <c>registration_caches</c>.
  /// Not serialized into the JSON payload.
  /// </summary>
  [JsonIgnore]
  public long ConcurrencyVersion { get; set; }

  #region Master data Properties

  /// <summary>
  /// Aggregate identity (same as <see cref="DomainDocumentBase{Guid}.Id"/>). Serialized as <c>AccountId</c> for DB/JSON compatibility.
  /// </summary>
  [JsonProperty("AccountId", Order = 1)]
  public Guid AccountId {
    get => Id;
    set => Id = value;
  }

  /// <summary>
  /// ACME directory account resource id (string), not the aggregate <see cref="DomainDocumentBase{Guid}.Id"/>.
  /// </summary>
  [JsonProperty("Id", Order = 2)]
  public string? AcmeAccountResourceId { get; set; }

  /// <summary>
  /// Human-readable label for this registration.
  /// </summary>
  [JsonProperty(Order = 3)]
  public required string Description { get; set; }

  /// <summary>
  /// ACME contact URLs (e.g. <c>mailto:</c>).
  /// </summary>
  [JsonProperty(Order = 4)]
  public required string[] Contacts { get; set; }

  /// <summary>
  /// True when using a staging ACME directory.
  /// </summary>
  [JsonProperty(Order = 5)]
  public required bool IsStaging { get; set; }

  /// <summary>
  /// Challenge type used for this account (e.g. http-01).
  /// </summary>
  [JsonProperty(Order = 6)]
  public required string ChallengeType { get; set; }

  /// <summary>
  /// When true, automated renewal / deploy should skip this account.
  /// </summary>
  [JsonProperty(Order = 7)]
  public bool IsDisabled { get; set; }

  #endregion

  #region ACME account keys

  /// <summary>
  /// Exported account key material (CSP blob) for the ACME account key pair.
  /// </summary>
  [JsonProperty(Order = 8)]
  public byte[]? AccountKey { get; set; }

  /// <summary>
  /// JWK representation of the account key (when provided by the directory).
  /// </summary>
  [JsonProperty(Order = 9)]
  public Jwk? Key { get; set; }

  /// <summary>
  /// Account resource location URI from the ACME directory.
  /// </summary>
  [JsonProperty(Order = 10)]
  public Uri? Location { get; set; }

  #endregion

  #region Cached certificates

  /// <summary>
  /// PEM and key material per DNS hostname.
  /// </summary>
  [JsonProperty(Order = 11)]
  public Dictionary<string, CertificateCache>? CachedCerts { get; set; }

  /// <summary>
  /// Earliest UTC instant when renewal may be attempted again for each hostname (e.g. after ACME rate limit).
  /// Keys use the same DNS names as <see cref="CachedCerts"/>.
  /// </summary>
  [JsonProperty(Order = 12)]
  public Dictionary<string, DateTimeOffset>? AcmeRenewalNotBeforeUtcByHostname { get; set; }

  #endregion

  #region Persistence constructor

  /// <summary>
  /// Parameterless constructor for JSON deserialization.
  /// </summary>
  public RegistrationCache() : base(Guid.Empty) { }

  #endregion

  #region Fluent API for setting properties

  /// <summary>
  /// Sets <see cref="AcmeAccountResourceId"/>.
  /// </summary>
  public RegistrationCache SetAcmeAccountResourceId(string? acmeAccountResourceId) {
    AcmeAccountResourceId = acmeAccountResourceId;
    return this;
  }

  /// <summary>
  /// Sets <see cref="Description"/>.
  /// </summary>
  public RegistrationCache SetDescription(string description) {
    Description = description;
    return this;
  }

  /// <summary>
  /// Sets <see cref="Contacts"/>.
  /// </summary>
  public RegistrationCache SetContacts(string[] contacts) {
    Contacts = contacts;
    return this;
  }

  /// <summary>
  /// Sets <see cref="IsStaging"/>.
  /// </summary>
  public RegistrationCache SetIsStaging(bool isStaging) {
    IsStaging = isStaging;
    return this;
  }

  /// <summary>
  /// Sets <see cref="ChallengeType"/>.
  /// </summary>
  public RegistrationCache SetChallengeType(string challengeType) {
    ChallengeType = challengeType;
    return this;
  }

  /// <summary>
  /// Sets <see cref="IsDisabled"/>.
  /// </summary>
  public RegistrationCache SetIsDisabled(bool isDisabled) {
    IsDisabled = isDisabled;
    return this;
  }

  /// <summary>
  /// Sets <see cref="AccountKey"/>.
  /// </summary>
  public RegistrationCache SetAccountKey(byte[]? accountKey) {
    AccountKey = accountKey;
    return this;
  }

  /// <summary>
  /// Sets <see cref="Key"/>.
  /// </summary>
  public RegistrationCache SetKey(Jwk? key) {
    Key = key;
    return this;
  }

  /// <summary>
  /// Sets <see cref="Location"/>.
  /// </summary>
  public RegistrationCache SetLocation(Uri? location) {
    Location = location;
    return this;
  }

  /// <summary>
  /// Replaces <see cref="CachedCerts"/>.
  /// </summary>
  public RegistrationCache SetCachedCerts(Dictionary<string, CertificateCache>? cachedCerts) {
    CachedCerts = cachedCerts;
    return this;
  }

  /// <summary>
  /// Replaces <see cref="AcmeRenewalNotBeforeUtcByHostname"/>.
  /// </summary>
  public RegistrationCache SetAcmeRenewalNotBeforeUtcByHostname(Dictionary<string, DateTimeOffset>? map) {
    AcmeRenewalNotBeforeUtcByHostname = map;
    return this;
  }

  #endregion

  #region Methods

  /// <summary>
  /// Returns hostnames whose certificate expires within <paramref name="days"/> days.
  /// </summary>
  public string[] GetHostsWithUpcomingSslExpiry(int days = 30) {
    var hostsWithUpcomingSslExpiry = new List<string>();

    if (CachedCerts == null)
      return hostsWithUpcomingSslExpiry.ToArray();

    foreach (var result in CachedCerts) {
      var (subject, cachedCert) = result;

      if (cachedCert.Cert != null && !cachedCert.IsDisabled) {
        using var cert = X509CertificateLoader.LoadCertificate(Encoding.ASCII.GetBytes(cachedCert.Cert));

        if ((cert.NotAfter - DateTime.UtcNow).TotalDays < days)
          hostsWithUpcomingSslExpiry.Add(subject);
      }
    }

    return hostsWithUpcomingSslExpiry.ToArray();
  }

  /// <summary>
  /// Builds read-model rows for UI from <see cref="CachedCerts"/>.
  /// </summary>
  public CachedHostname[] GetHosts() {
    if (CachedCerts == null)
      return [];

    var hosts = new List<CachedHostname>();

    foreach (var result in CachedCerts) {
      var (subject, cachedCert) = result;

      if (cachedCert.Cert != null) {
        using var cert = X509CertificateLoader.LoadCertificate(Encoding.ASCII.GetBytes(cachedCert.Cert));

        hosts.Add(new CachedHostname(
          CombGui.GenerateCombGuid(),
          subject,
          cert.NotAfter,
          (cert.NotAfter - DateTime.UtcNow).TotalDays < 30,
          cachedCert.IsDisabled
        ));
      }
    }

    return hosts.ToArray();
  }

  /// <summary>
  /// Returns true when <paramref name="hostname"/> is under ACME renewal cooldown.
  /// </summary>
  public bool IsHostnameInAcmeCooldown(string hostname, out DateTimeOffset notBeforeUtc) {
    notBeforeUtc = default;
    if (AcmeRenewalNotBeforeUtcByHostname == null)
      return false;

    foreach (var kvp in AcmeRenewalNotBeforeUtcByHostname) {
      if (!string.Equals(kvp.Key, hostname, StringComparison.OrdinalIgnoreCase))
        continue;
      if (kvp.Value <= DateTimeOffset.UtcNow)
        return false;
      notBeforeUtc = kvp.Value;
      return true;
    }

    return false;
  }

  /// <summary>
  /// Clears cooldown metadata for a hostname after a successful renewal attempt.
  /// </summary>
  public void ClearAcmeCooldownForHostname(string hostname) {
    if (AcmeRenewalNotBeforeUtcByHostname == null)
      return;

    var key = AcmeRenewalNotBeforeUtcByHostname.Keys
      .FirstOrDefault(k => string.Equals(k, hostname, StringComparison.OrdinalIgnoreCase));
    if (key != null)
      AcmeRenewalNotBeforeUtcByHostname.Remove(key);
  }

  /// <summary>
  /// Returns a clone of the cached cert with re-exported private key material when still valid.
  /// </summary>
  public bool TryGetCachedCertificate(string subject, out CertificateCache? value) {
    value = null;

    if (CachedCerts == null)
      return false;

    if (!CachedCerts.TryGetValue(subject, out var cache))
      return false;

    using var cert = X509CertificateLoader.LoadCertificate(Encoding.ASCII.GetBytes(cache.Cert));

    if ((cert.NotAfter - DateTime.UtcNow).TotalDays < 30)
      return false;

    if (cache.Private is null)
      return false;

    var rsa = new RSACryptoServiceProvider(4096);
    rsa.ImportCspBlob(cache.Private);

    value = new CertificateCache(
      cache.Id,
      cache.Cert,
      rsa.ExportCspBlob(true),
      rsa.ExportRSAPrivateKeyPem(),
      cache.IsDisabled
    );

    return true;
  }

  /// <summary>
  /// Removes cached certificate rows for the given hostnames.
  /// </summary>
  public void ResetCachedCertificate(IEnumerable<string> hostsToRemove) {
    if (CachedCerts != null)
      foreach (var host in hostsToRemove)
        CachedCerts.Remove(host);
  }

  /// <summary>
  /// Concatenated PEM bundle per hostname for agent upload.
  /// </summary>
  public Dictionary<string, string> GetCertsPemPerHostname() {
    var result = new Dictionary<string, string>();
    if (CachedCerts == null)
      return result;

    foreach (var kvp in CachedCerts) {
      var hostname = kvp.Key;
      var cert = kvp.Value;
      if (!string.IsNullOrEmpty(cert.Cert) && !string.IsNullOrEmpty(cert.PrivatePem))
        result[hostname] = $"{cert.Cert}\n{cert.PrivatePem}";
    }

    return result;
  }

  #endregion
}
