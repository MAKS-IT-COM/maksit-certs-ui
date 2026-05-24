namespace MaksIT.CertsUI.Client;

/// <summary>Thrown when the CertsUI API returns a non-success status code.</summary>
public class CertsUIApiException : Exception {
  public int StatusCode { get; }
  public string? ResponseBody { get; }

  public CertsUIApiException(int statusCode, string? message = null, string? responseBody = null, Exception? inner = null)
    : base(FormatMessage(statusCode, message, responseBody), inner) {
    StatusCode = statusCode;
    ResponseBody = responseBody;
  }

  private static string FormatMessage(int statusCode, string? message, string? responseBody) {
    var m = message ?? $"CertsUI API returned {statusCode}.";
    if (string.IsNullOrWhiteSpace(responseBody)) return m;
    const int maxLen = 500;
    var snippet = responseBody.Length > maxLen ? responseBody[..maxLen] + "…" : responseBody;
    return $"{m} Response: {snippet}";
  }
}
