using System.Net;
using Microsoft.Extensions.Options;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Client;
using MaksIT.CertsUI.Client.Models;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client.Tests;

public class CertsUIClientTests {
  private const string BaseAddress = "https://certsui.test";
  private const string ApiKey = "test-api-key-12345";

  private static (CertsUIClient Client, FakeHttpMessageHandler Handler) CreateClient() {
    var handler = new FakeHttpMessageHandler();
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress + "/") };
    var options = Options.Create(new CertsUIClientOptions { BaseAddress = BaseAddress, ApiKey = ApiKey });
    var client = new CertsUIClient(httpClient, options);
    return (client, handler);
  }

  private static (CertsUIClient Client, FakeHttpMessageHandler Handler) CreateClientWithManualCtor() {
    var handler = new FakeHttpMessageHandler();
    var httpClient = new HttpClient(handler);
    var client = new CertsUIClient(httpClient, BaseAddress, ApiKey);
    return (client, handler);
  }

  private static string Json(object obj) => obj.ToJson();

  [Fact]
  public async Task Ctor_SetsApiKeyHeader() {
    var (client, handler) = CreateClient();
    var accountId = Guid.NewGuid();
    handler.SetResponse(HttpStatusCode.OK, Json(new {
      accountId,
      isDisabled = false,
      description = "x",
      contacts = new[] { "mailto:a@b" },
      challengeType = "http-01",
      isStaging = false,
    }));

    await client.GetAccountAsync(accountId, TestContext.Current.CancellationToken);

    Assert.NotNull(handler.LastRequest);
    Assert.True(handler.LastRequest!.Headers.TryGetValues("X-API-KEY", out var values));
    Assert.Equal(ApiKey, values!.Single());
  }

  [Fact]
  public async Task GetAccountAsync_SendsCorrectRequest() {
    var (client, handler) = CreateClient();
    var accountId = Guid.NewGuid();
    var expected = new {
      accountId,
      isDisabled = false,
      description = "My account",
      contacts = new[] { "mailto:a@b" },
      challengeType = "http-01",
      isStaging = false,
    };
    handler.SetResponse(HttpStatusCode.OK, Json(expected));

    var result = await client.GetAccountAsync(accountId, TestContext.Current.CancellationToken);

    Assert.Equal(accountId, result.AccountId);
    Assert.Equal("My account", result.Description);
    Assert.NotNull(handler.LastRequest);
    Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    Assert.Equal($"https://certsui.test/api/account/{accountId:D}", handler.LastRequest.RequestUri!.ToString());
  }

  [Fact]
  public async Task GetAccountAsync_WhenApiReturns404_ThrowsCertsUIApiException() {
    var (client, handler) = CreateClient();
    handler.SetResponse(HttpStatusCode.NotFound, "{\"detail\":\"Not found\"}");

    var ex = await Assert.ThrowsAsync<CertsUIApiException>(() =>
      client.GetAccountAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

    Assert.Equal(404, ex.StatusCode);
    Assert.Contains("Not found", ex.ResponseBody ?? "");
  }

  [Fact]
  public async Task GetAccountsAsync_DeserializesArray() {
    var (client, handler) = CreateClient();
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();
    handler.SetResponse(HttpStatusCode.OK, Json(new[] {
      new { accountId = id1, isDisabled = false, contacts = new[] { "a" }, isStaging = false },
      new { accountId = id2, isDisabled = true, contacts = new[] { "b" }, isStaging = true },
    }));

    var result = await client.GetAccountsAsync(TestContext.Current.CancellationToken);

    Assert.Equal(2, result.Length);
    Assert.Equal(id1, result[0].AccountId);
    Assert.Equal(id2, result[1].AccountId);
    Assert.Equal("https://certsui.test/api/accounts", handler.LastRequest!.RequestUri!.ToString());
  }

  [Fact]
  public async Task CreateAccountAsync_SendsPostWithBody() {
    var (client, handler) = CreateClient();
    var accountId = Guid.NewGuid();
    handler.SetResponse(HttpStatusCode.OK, Json(new {
      accountId,
      isDisabled = false,
      description = "New",
      contacts = new[] { "mailto:x@y" },
      challengeType = "http-01",
      hostnames = Array.Empty<object>(),
      isStaging = true,
    }));

    var result = await client.CreateAccountAsync(new PostAccountRequest {
      Description = "New",
      Contacts = ["mailto:x@y"],
      ChallengeType = "http-01",
      Hostnames = ["example.com"],
      IsStaging = true,
      AgreeToS = true,
    }, TestContext.Current.CancellationToken);

    Assert.Equal(accountId, result.AccountId);
    Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    Assert.Equal("https://certsui.test/api/account", handler.LastRequest.RequestUri!.ToString());
  }

  [Fact]
  public async Task GetRuntimeInstanceIdAsync_Deserializes() {
    var (client, handler) = CreateClient();
    handler.SetResponse(HttpStatusCode.OK, Json(new { instanceId = "pod-abc" }));

    var result = await client.GetRuntimeInstanceIdAsync(TestContext.Current.CancellationToken);

    Assert.Equal("pod-abc", result.InstanceId);
    Assert.Contains("/api/debug/runtime-instance-id", handler.LastRequest!.RequestUri!.ToString());
  }

  [Fact]
  public async Task CheckHealthLiveAsync_HitsHealthEndpoint() {
    var (client, handler) = CreateClient();
    handler.SetResponse(HttpStatusCode.OK);

    await client.CheckHealthLiveAsync(TestContext.Current.CancellationToken);

    Assert.Equal("https://certsui.test/health/live", handler.LastRequest!.RequestUri!.ToString());
  }

  [Fact]
  public async Task ManualCtor_WithBaseAddressAndApiKey_Works() {
    var (client, handler) = CreateClientWithManualCtor();
    handler.SetResponse(HttpStatusCode.OK, Json(Array.Empty<object>()));

    var result = await client.GetAccountsAsync(TestContext.Current.CancellationToken);

    Assert.NotNull(result);
    Assert.True(handler.LastRequest!.Headers.TryGetValues("X-API-KEY", out var v));
    Assert.Equal(ApiKey, v!.Single());
  }
}
