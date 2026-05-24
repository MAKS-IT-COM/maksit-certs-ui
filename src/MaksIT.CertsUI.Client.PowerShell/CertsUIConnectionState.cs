using MaksIT.CertsUI.Client;

namespace MaksIT.CertsUI.Client.PowerShell;

/// <summary>Holds the current CertsUI client and connection info for the PowerShell session.</summary>
internal static class CertsUIConnectionState {
  private static readonly object Lock = new();
  private static ICertsUIClient? _client;
  private static HttpClient? _httpClient;

  public static ICertsUIClient? Client => _client;

  public static void SetConnection(string baseAddress, string apiKey) {
    lock (Lock) {
      _httpClient?.Dispose();
      _httpClient = new HttpClient();
      _client = new CertsUIClient(_httpClient, baseAddress, apiKey);
    }
  }

  public static void ClearConnection() {
    lock (Lock) {
      _client = null;
      _httpClient?.Dispose();
      _httpClient = null;
    }
  }
}
