using System.Net;
using System.Text.Json;
using Xunit;

namespace MaksIT.CertsUI.Tests.Services;

/// <summary>
/// E2E tests against a live CertsUI deployment.
/// Set CERTSUI_E2E_BASE_URL and CERTSUI_E2E_API_KEY to enable.
/// </summary>
public sealed class CertsUiApiKeyE2ETests {
  private static void Log(string message) =>
    Console.WriteLine($"[{DateTime.UtcNow:O}] {message}");

  private sealed class E2ESession : IDisposable {
    internal E2ESession(HttpClient http) {
      Http = http;
    }

    internal HttpClient Http { get; }
    public void Dispose() => Http.Dispose();
  }

  private static E2ESession? TryCreateSession() {
    var baseUrl = Environment.GetEnvironmentVariable("CERTSUI_E2E_BASE_URL");
    var apiKey = Environment.GetEnvironmentVariable("CERTSUI_E2E_API_KEY");
    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
      return null;

    var http = new HttpClient {
      BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
    };
    http.DefaultRequestHeaders.Remove("X-API-KEY");
    http.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
    return new E2ESession(http);
  }

  [Fact]
  [Trait("Category", "E2E")]
  public async Task HealthEndpoints_LiveAndReady_ReturnSuccess() {
    using var session = TryCreateSession();
    if (session is null) {
      Log("SKIP: CERTSUI_E2E_BASE_URL or CERTSUI_E2E_API_KEY not set.");
      return;
    }

    var live = await session.Http.GetAsync("health/live", CancellationToken.None);
    var ready = await session.Http.GetAsync("health/ready", CancellationToken.None);

    Assert.True(live.IsSuccessStatusCode, $"Expected /health/live to be successful, got {(int)live.StatusCode}.");
    Assert.True(ready.IsSuccessStatusCode, $"Expected /health/ready to be successful, got {(int)ready.StatusCode}.");
    Log("PASS: /health/live and /health/ready are healthy.");
  }

  [Fact]
  [Trait("Category", "E2E")]
  public async Task ApiKey_ConcurrentReads_OnAccountsEndpoint_AreAuthorizedAndStable() {
    using var session = TryCreateSession();
    if (session is null) {
      Log("SKIP: CERTSUI_E2E_BASE_URL or CERTSUI_E2E_API_KEY not set.");
      return;
    }

    const int parallelRequests = 12;
    Log($"TEST: issuing {parallelRequests} concurrent GET /api/accounts requests with API key.");

    var calls = Enumerable.Range(0, parallelRequests)
      .Select(_ => session.Http.GetAsync("api/accounts", CancellationToken.None));
    var responses = await Task.WhenAll(calls);

    foreach (var response in responses) {
      Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
      Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
      Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}.");
    }

    Log("PASS: all concurrent API-key reads succeeded without auth failures.");
  }

  [Fact]
  [Trait("Category", "E2E")]
  public async Task ApiKey_StickyLessRequests_RuntimeInstanceId_ObservesMultipleReplicas() {
    using var session = TryCreateSession();
    if (session is null) {
      Log("SKIP: CERTSUI_E2E_BASE_URL or CERTSUI_E2E_API_KEY not set.");
      return;
    }

    var minDistinct = 2;
    var minDistinctRaw = Environment.GetEnvironmentVariable("CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES");
    if (!string.IsNullOrWhiteSpace(minDistinctRaw) &&
        int.TryParse(minDistinctRaw, out var parsed) &&
        parsed > 0) {
      minDistinct = parsed;
    }

    const int requestCount = 32;
    Log($"TEST: issuing {requestCount} requests to /api/debug/runtime-instance-id; expected distinct instances >= {minDistinct}.");

    var responses = await Task.WhenAll(
      Enumerable.Range(0, requestCount)
        .Select(_ => session.Http.GetAsync("api/debug/runtime-instance-id", CancellationToken.None)));

    var instanceIds = new HashSet<string>(StringComparer.Ordinal);
    foreach (var response in responses) {
      Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}.");
      var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);
      using var doc = JsonDocument.Parse(payload);
      var id = doc.RootElement.GetProperty("instanceId").GetString();
      Assert.False(string.IsNullOrWhiteSpace(id));
      instanceIds.Add(id!);
    }

    Log($"Observed distinct instance ids: {string.Join(", ", instanceIds.OrderBy(x => x))}");
    Assert.True(
      instanceIds.Count >= minDistinct,
      $"Expected at least {minDistinct} distinct instance ids, observed {instanceIds.Count}. " +
      "Scale server replicas and ensure non-sticky load balancing for this E2E run.");
    Log("PASS: runtime instance id endpoint observed multiple replicas.");
  }
}

