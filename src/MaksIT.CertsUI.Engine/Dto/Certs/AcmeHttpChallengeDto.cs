namespace MaksIT.CertsUI.Engine.Dto.Certs;

/// <summary>PostgreSQL <c>acme_http_challenges</c> row: HTTP-01 key authorization keyed by token filename.</summary>
public class AcmeHttpChallengeDto {
  public string FileName { get; set; } = "";
  public string TokenValue { get; set; } = "";
  public DateTimeOffset CreatedAtUtc { get; set; }
}
