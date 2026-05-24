using Microsoft.Extensions.Options;
using MaksIT.CertsUI.Client;

namespace MaksIT.CertsUI.Client.Tests;

/// <summary>E2E tests against a live CertsUI deployment (set <c>CERTSUI_E2E_BASE_URL</c> and <c>CERTSUI_E2E_API_KEY</c>).</summary>
public sealed class CertsUiApiKeyE2ETests {
  private static void Log(string message) =>
    Console.WriteLine($"[{DateTime.UtcNow:O}] {message}");

  private sealed class E2ESession(HttpClient http, CertsUIClient client) : IDisposable {
    internal CertsUIClient Client { get; } = client;
    public void Dispose() => http.Dispose();
  }

  private static E2ESession? TryCreateSession() {
    var baseUrl = Environment.GetEnvironmentVariable("CERTSUI_E2E_BASE_URL");
    var apiKey = Environment.GetEnvironmentVariable("CERTSUI_E2E_API_KEY");
    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
      return null;

    var http = new HttpClient();
    var client = new CertsUIClient(http, Options.Create(new CertsUIClientOptions {
      BaseAddress = baseUrl,
      ApiKey = apiKey,
    }));
    return new E2ESession(http, client);
  }

  [Fact]
  [Trait("Category", "E2E")]
  public async Task HealthEndpoints_LiveAndReady_ReturnSuccess() {
    using var session = TryCreateSession();
    if (session is null) {
      Log("SKIP: CERTSUI_E2E_BASE_URL or CERTSUI_E2E_API_KEY not set.");
      return;
    }

    await session.Client.CheckHealthLiveAsync(TestContext.Current.CancellationToken);
    await session.Client.CheckHealthReadyAsync(TestContext.Current.CancellationToken);
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
    Log($"TEST: issuing {parallelRequests} concurrent GetAccountsAsync with API key.");

    var calls = Enumerable.Range(0, parallelRequests)
      .Select(_ => session.Client.GetAccountsAsync(TestContext.Current.CancellationToken));
    var results = await Task.WhenAll(calls);

    Assert.Equal(parallelRequests, results.Length);
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
    Log($"TEST: {requestCount} GetRuntimeInstanceIdAsync calls; expected distinct instances >= {minDistinct}.");

    var responses = await Task.WhenAll(
      Enumerable.Range(0, requestCount)
        .Select(_ => session.Client.GetRuntimeInstanceIdAsync(TestContext.Current.CancellationToken)));

    var instanceIds = new HashSet<string>(StringComparer.Ordinal);
    foreach (var response in responses) {
      Assert.False(string.IsNullOrWhiteSpace(response.InstanceId));
      instanceIds.Add(response.InstanceId);
    }

    Log($"Observed distinct instance ids: {string.Join(", ", instanceIds.OrderBy(x => x))}");
    Assert.True(
      instanceIds.Count >= minDistinct,
      $"Expected at least {minDistinct} distinct instance ids, observed {instanceIds.Count}. " +
      "Scale server replicas and ensure non-sticky load balancing for this E2E run.");
    Log("PASS: runtime instance id endpoint observed multiple replicas.");
  }
}
