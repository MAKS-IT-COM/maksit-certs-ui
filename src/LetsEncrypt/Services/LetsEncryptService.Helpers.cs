using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using MaksIT.Core.Extensions;
using MaksIT.Core.Security.JWS;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.LetsEncrypt.Entities.Jws;
using MaksIT.LetsEncrypt.Entities.LetsEncrypt;
using MaksIT.LetsEncrypt.Exceptions;
using MaksIT.LetsEncrypt.Models.Interfaces;
using MaksIT.LetsEncrypt.Models.Responses;
using MaksIT.Results;


namespace MaksIT.LetsEncrypt.Services;

public partial class LetsEncryptService {

  #region Internal helpers

  private State GetOrCreateState(Guid sessionId) => _sessions.GetOrCreate(sessionId);

  private async Task<Result<string?>> GetNonceAsync(Guid sessionId, Uri uri) {
    if (uri == null)
      return Result<string?>.InternalServerError(null, "URI is null");

    try {
      var state = GetOrCreateState(sessionId);

      _logger.LogInformation($"Executing {nameof(GetNonceAsync)}...");

      if (state.Directory is not { NewNonce: { } newNonceUri })
        return Result<string?>.InternalServerError(null, "Directory or NewNonce URL is null.");

      var result = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, newNonceUri));

      var nonce = result.Headers.GetValues(ReplayNonceHeader).FirstOrDefault();

      if (nonce == null)
        return Result<string?>.InternalServerError(null, "Nonce is null");

      return Result<string?>.Ok(nonce);
    }
    catch (Exception ex) {
      return HandleUnhandledException<string?>(ex);
    }
  }

  private async Task<Result<SendResult<T>?>> SendAcmeRequest<T>(HttpRequestMessage request, State state, HttpMethod method) {
    try {
      var response = await _httpClient.SendAsync(request);

      var responseText = await response.Content.ReadAsStringAsync();

      HandleProblemResponseAsync(response, responseText);

      var sendResult = ProcessResponseContent<T>(response, responseText);

      return Result<SendResult<T>?>.Ok(sendResult);
    }

    catch (LetsEncrytException ex) {
      return MapLetsEncryptException<SendResult<T>?>(state, ex, null);
    }
    catch (Exception ex) {
      return HandleUnhandledException<SendResult<T>?>(ex);
    }
  }

  private Result<string?> EncodeMessage(Guid sessionId, bool isPostAsGet, object? requestModel, ACMEJwsHeader protectedHeader) {
    var state = GetOrCreateState(sessionId);

    if (!state.TryGetAccountKey(out var rsa, out var jwk))
      return Result<string?>.InternalServerError(AccountKeyMissingMessage);

    if (isPostAsGet) {
      if (!JwsGenerator.TryEncode(rsa, jwk, protectedHeader, out var jwsPostAsGet, out var errPostAsGet))
        return Result<string?>.InternalServerError(errPostAsGet);

      return Result<string?>.Ok(jwsPostAsGet.ToJson());
    }

    if (!JwsGenerator.TryEncode(rsa, jwk, protectedHeader, requestModel, out var jwsWithPayload, out var errWithPayload))
      return Result<string?>.InternalServerError(errWithPayload);

    return Result<string?>.Ok(jwsWithPayload.ToJson());
  }

  private static string GetContentType(ContentType type) => type.GetDisplayName();

  private void PrepareRequestContent(HttpRequestMessage request, string json, HttpMethod method) {
    request.Content = new StringContent(json ?? string.Empty);
    var contentType = method == HttpMethod.Post
      ? GetContentType(ContentType.JoseJson)
      : GetContentType(ContentType.Json);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
  }

  private async Task<Result> PollChallengeStatus(Guid sessionId, AuthorizationChallengeChallenge challenge) {
    if (challenge?.Url == null)
      return Result.InternalServerError("Challenge URL is null");

    var start = DateTime.UtcNow;

    while (true) {
      var pollRequest = new HttpRequestMessage(HttpMethod.Post, challenge.Url);

      var nonceResult = await GetNonceAsync(sessionId, challenge.Url);
      if (!nonceResult.IsSuccess || nonceResult.Value == null)
        return nonceResult;

      var nonce = nonceResult.Value;

      var pollJsonResult = EncodeMessage(sessionId, true, null, new ACMEJwsHeader {
        Url = challenge.Url.ToString(),
        Nonce = nonce
      });

      if (!pollJsonResult.IsSuccess || pollJsonResult.Value == null)
        return pollJsonResult;

      var pollJson = pollJsonResult.Value;

      PrepareRequestContent(pollRequest, pollJson, HttpMethod.Post);

      var pollResponse = await _httpClient.SendAsync(pollRequest);

      var pollResponseText = await pollResponse.Content.ReadAsStringAsync();

      HandleProblemResponseAsync(pollResponse, pollResponseText);

      var authChallenge = ProcessResponseContent<AuthorizationChallengeResponse>(pollResponse, pollResponseText);

      if (authChallenge.Result?.Status != "pending")
        return authChallenge.Result?.Status == "valid" ? Result.Ok() : Result.InternalServerError();

      if ((DateTime.UtcNow - start).Seconds > 120)
        return Result.InternalServerError("Timeout");

      await Task.Delay(1000);
    }
  }

  private void HandleProblemResponseAsync(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.ProblemJson)) {
      var problem = responseText.ToObject<Problem>();

      throw new LetsEncrytException(problem, response);
    }

    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.Json)) {
      var authorizationChallengeChallenge = responseText.ToObject<AuthorizationChallengeChallenge>();

      if (authorizationChallengeChallenge?.Status == "invalid") {
        throw new LetsEncrytException(new Problem {
          Type = authorizationChallengeChallenge.Error?.Type,
          Detail = authorizationChallengeChallenge.Error?.Detail,
          RawJson = responseText
        }, response);
      }
    }
  }

  private SendResult<TResult> ProcessResponseContent<TResult>(HttpResponseMessage response, string responseText) {
    if (response.Content.Headers.ContentType?.MediaType == GetContentType(ContentType.PemCertificateChain) && typeof(TResult) == typeof(string)) {
      return new SendResult<TResult> {
        Result = (TResult)(object)responseText
      };
    }
    var responseContent = responseText.ToObject<TResult>();
    if (responseContent is IHasLocation ihl && response.Headers.Location != null) {
      ihl.Location = response.Headers.Location;
    }
    return new SendResult<TResult> {
      Result = responseContent,
      ResponseText = responseText
    };
  }

  private static bool StatusEquals(string? status, OrderStatus expected) => status == expected.GetDisplayName();

  private Result MapLetsEncryptException(State state, LetsEncrytException ex) =>
    MapLetsEncryptExceptionCore<Result>(state, ex, m => Result.TooManyRequests(m), m => Result.InternalServerError(m));

  private Result<T?> MapLetsEncryptException<T>(State state, LetsEncrytException ex, T? defaultValue) =>
    MapLetsEncryptExceptionCore(state, ex, m => Result<T?>.TooManyRequests(defaultValue, m), m => Result<T?>.InternalServerError(defaultValue, m));

  private TResult MapLetsEncryptExceptionCore<TResult>(
      State state,
      LetsEncrytException ex,
      Func<string, TResult> tooManyRequests,
      Func<string, TResult> internalError) {
    try {
      if (ex.ProblemKind == AcmeProblemKind.RateLimited) {
        var when = ex.RetryAfterUtc ?? DateTimeOffset.UtcNow.AddHours(1);
        var id = ex.RateLimitedIdentifier;
        if (state.Cache != null && !string.IsNullOrEmpty(id)) {
          var key = id.ToLowerInvariant();
          state.Cache.AcmeRenewalNotBeforeUtcByHostname ??= new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
          if (state.Cache.AcmeRenewalNotBeforeUtcByHostname.TryGetValue(key, out var existing))
            when = when > existing ? when : existing;
          state.Cache.AcmeRenewalNotBeforeUtcByHostname[key] = when;
        }

        _logger.LogWarning(
            "ACME rate limited: Kind {AcmeProblemKind}, Type {AcmeProblemType}, Hostname {Hostname}, RetryNotBefore {RetryNotBeforeUtc:o}. Detail: {Detail}",
            ex.ProblemKind, ex.Problem?.Type, id, when, ex.Problem?.Detail);

        var msg = string.IsNullOrEmpty(id)
          ? $"Let's Encrypt rate limit. Do not retry certificate operations before {when:u} UTC."
          : $"Let's Encrypt rate limit for hostname '{id}'. Do not retry before {when:u} UTC.";
        return tooManyRequests(msg);
      }

      _logger.LogWarning(ex,
          "Let's Encrypt ACME problem: Kind {AcmeProblemKind}, Type {AcmeProblemType}. {Detail}",
          ex.ProblemKind, ex.Problem?.Type, ex.Problem?.Detail);
      var fallback = string.IsNullOrEmpty(ex.Message) ? "Let's Encrypt request failed." : ex.Message;
      return internalError(fallback);
    }
    finally {
      ex.Response?.Dispose();
    }
  }

  private Result HandleUnhandledException(Exception ex, string defaultMessage = "Let's Encrypt client unhandled exception") {
    _logger.LogError(ex, defaultMessage);
    return Result.InternalServerError([defaultMessage, .. ex.ExtractMessages()]);
  }

  private Result<T?> HandleUnhandledException<T>(Exception ex, T? defaultValue = default, string defaultMessage = "Let's Encrypt client unhandled exception") {
    _logger.LogError(ex, defaultMessage);
    return Result<T?>.InternalServerError(defaultValue, [.. ex.ExtractMessages()]);
  }
  #endregion
}
