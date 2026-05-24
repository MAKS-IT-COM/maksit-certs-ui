namespace MaksIT.CertsUI.Client;

/// <summary>Options for <see cref="CertsUIClient"/>: base address and API key.</summary>
public class CertsUIClientOptions {
  /// <summary>Base URL of the CertsUI deployment (e.g. https://certs-ui.example.com or http://localhost:8080). No trailing slash.</summary>
  public required string BaseAddress { get; set; }

  /// <summary>API key sent in the X-API-KEY header.</summary>
  public required string ApiKey { get; set; }
}
