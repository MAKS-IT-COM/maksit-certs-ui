using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Client.Models;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client;

/// <summary>HTTP client for the MaksIT CertsUI API using API key authentication.</summary>
public class CertsUIClient : ICertsUIClient {
  private const string ApiKeyHeaderName = "X-API-KEY";
  private const string ApiBasePath = "api";

  private static readonly JsonSerializerOptions JsonOptions = new() {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private readonly HttpClient _httpClient;

  public CertsUIClient(HttpClient httpClient, IOptions<CertsUIClientOptions> options) {
    _httpClient = httpClient;
    var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
    ConfigureClient(opts.BaseAddress, opts.ApiKey);
  }

  /// <summary>Constructor for use with manually configured HttpClient (e.g. in tests or PowerShell).</summary>
  public CertsUIClient(HttpClient httpClient, string baseAddress, string apiKey) {
    _httpClient = httpClient;
    ConfigureClient(baseAddress, apiKey);
  }

  private void ConfigureClient(string baseAddress, string apiKey) {
    var baseUri = baseAddress.TrimEnd('/');
    _httpClient.BaseAddress = new Uri(baseUri + "/");
    _httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeaderName, apiKey);
  }

  public async Task CheckHealthLiveAsync(CancellationToken cancellationToken = default) {
    var response = await _httpClient.GetAsync("health/live", cancellationToken).ConfigureAwait(false);
    await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
  }

  public async Task CheckHealthReadyAsync(CancellationToken cancellationToken = default) {
    var response = await _httpClient.GetAsync("health/ready", cancellationToken).ConfigureAwait(false);
    await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
  }

  public Task<AccountResponse[]> GetAccountsAsync(CancellationToken cancellationToken = default) =>
    GetAsync<AccountResponse[]>($"{ApiBasePath}/accounts", cancellationToken);

  public Task<AccountResponse> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default) =>
    GetAsync<AccountResponse>($"{ApiBasePath}/account/{accountId:D}", cancellationToken);

  public Task<AccountResponse> CreateAccountAsync(PostAccountRequest request, CancellationToken cancellationToken = default) =>
    PostAsync<PostAccountRequest, AccountResponse>($"{ApiBasePath}/account", request, cancellationToken);

  public Task<AccountResponse> PatchAccountAsync(Guid accountId, PatchAccountRequest request, CancellationToken cancellationToken = default) =>
    PatchAsync<PatchAccountRequest, AccountResponse>($"{ApiBasePath}/account/{accountId:D}", request, cancellationToken);

  public async Task DeleteAccountAsync(Guid accountId, CancellationToken cancellationToken = default) {
    var response = await _httpClient.DeleteAsync($"{ApiBasePath}/account/{accountId:D}", cancellationToken).ConfigureAwait(false);
    await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
  }

  public Task<RuntimeInstanceIdResponse> GetRuntimeInstanceIdAsync(CancellationToken cancellationToken = default) =>
    GetAsync<RuntimeInstanceIdResponse>($"{ApiBasePath}/debug/runtime-instance-id", cancellationToken);

  private async Task<T> GetAsync<T>(string path, CancellationToken ct) where T : class {
    var response = await _httpClient.GetAsync(path, ct).ConfigureAwait(false);
    await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
    var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
    return result ?? throw new CertsUIApiException((int)response.StatusCode, "Response body was null.");
  }

  private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    where TResponse : class {
    var response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, ct).ConfigureAwait(false);
    await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
    var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct).ConfigureAwait(false);
    return result ?? throw new CertsUIApiException((int)response.StatusCode, "Response body was null.");
  }

  private async Task<TResponse> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    where TResponse : class {
    var response = await _httpClient.PatchAsJsonAsync(path, body, JsonOptions, ct).ConfigureAwait(false);
    await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
    var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct).ConfigureAwait(false);
    return result ?? throw new CertsUIApiException((int)response.StatusCode, "Response body was null.");
  }

  private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct) {
    if (response.IsSuccessStatusCode) return;
    var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    throw new CertsUIApiException((int)response.StatusCode, null, body);
  }
}
