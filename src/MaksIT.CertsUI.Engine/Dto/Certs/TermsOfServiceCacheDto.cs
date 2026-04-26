namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>PostgreSQL <c>terms_of_service_cache</c> row keyed by Terms of Service URL.</summary>
public class TermsOfServiceCacheDto {
  public string Url { get; set; } = "";
  public string UrlHashHex { get; set; } = "";
  public string? ETag { get; set; }
  public DateTimeOffset? LastModifiedUtc { get; set; }
  public string ContentType { get; set; } = "application/pdf";
  public byte[] ContentBytes { get; set; } = [];
  public DateTimeOffset FetchedAtUtc { get; set; }
  public DateTimeOffset ExpiresAtUtc { get; set; }
}
