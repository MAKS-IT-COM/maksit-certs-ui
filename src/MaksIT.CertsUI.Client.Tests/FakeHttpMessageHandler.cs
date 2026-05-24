using System.Net;

namespace MaksIT.CertsUI.Client.Tests;

/// <summary>Delegating handler that returns a configured response and optionally records the request.</summary>
public class FakeHttpMessageHandler : DelegatingHandler {
  private HttpResponseMessage? _responseToReturn;

  public HttpRequestMessage? LastRequest { get; private set; }

  public void SetResponse(HttpResponseMessage response) {
    _responseToReturn = response;
  }

  public void SetResponse(HttpStatusCode status, string? jsonContent = null) {
    _responseToReturn = new HttpResponseMessage(status) {
      Content = jsonContent != null ? new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json") : null,
    };
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
    LastRequest = request;
    if (_responseToReturn != null)
      return _responseToReturn;
    return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
  }
}
