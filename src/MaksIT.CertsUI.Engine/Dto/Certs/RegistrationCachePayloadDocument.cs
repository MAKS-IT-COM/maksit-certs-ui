using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaksIT.Core.Security.JWK;
using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

namespace MaksIT.CertsUI.Engine.Dto.Certs;

public sealed class RegistrationCachePayloadDocument {

  private Guid? _id;
  public Guid Id { get => _id ?? AccountId; set => _id = value; }

  public Guid AccountId { get; set; }

  /// <summary>Filled from JSON key <c>Id</c> (ACME account URI).</summary>
  public string? RootIdCapital { get; set; }

  /// <summary>Filled from JSON key <c>id</c>.</summary>
  public string? RootIdLowercase { get; set; }

  /// <summary>Optional key <c>acmeAccountResourceId</c> when present.</summary>
  public string? AcmeAccountResourceId { get; set; }

  public string? Description { get; set; }

  public string[]? Contacts { get; set; }

  public bool IsStaging { get; set; }

  public string? ChallengeType { get; set; }

  public bool IsDisabled { get; set; }

  public byte[]? AccountKey { get; set; }

  public Jwk? Key { get; set; }

  public Uri? Location { get; set; }

  public Dictionary<string, CertificateCache>? CachedCerts { get; set; }

  /// <inheritdoc cref="RegistrationCache.AcmeRenewalNotBeforeUtcByHostname" />
  public Dictionary<string, DateTimeOffset>? AcmeRenewalNotBeforeUtcByHostname { get; set; }
}
