using MaksIT.CertsUI;
using MaksIT.CertsUI.Authorization;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Abstractions.Services;

public abstract class ServiceBase(ILogger logger, IOptions<Configuration> appSettings)
{

  protected readonly ILogger _logger = logger;
  protected readonly Configuration _appSettings = appSettings.Value;

  protected Result UnsupportedPatchOperationResponse()
  {
    return Result.BadRequest("Unsupported operation");
  }

  protected Result<T?> UnsupportedPatchOperationResponse<T>()
  {
    return Result<T?>.BadRequest(default, "Unsupported operation");
  }

  protected Result PatchFieldIsNotDefined(string fieldName)
  {
    return Result.BadRequest($"It's not possible to set non defined field {fieldName}.");
  }

  protected Result<T?> PatchFieldIsNotDefined<T>(string fieldName)
  {
    return Result<T?>.BadRequest(default, $"It's not possible to set non defined field {fieldName}.");
  }
  
  protected Result NoValidAuthorizationMethod() {
    _logger.LogInformation("Access denied: No valid authorization method available.");
    return Result.Forbidden("No valid authorization method available.");
  }

  protected Result<T?> NoValidAuthorizationMethod<T>() {
    _logger.LogInformation("Access denied: No valid authorization method available.");
    return Result<T?>.Forbidden(default, "No valid authorization method available.");
  }

  /// <summary>
  /// Placeholder aligned with Vault <c>ServiceBase.RBACWrapper</c>; pass data from
  /// <c>MaksIT.CertsUI.Authorization.Extensions.HttpContextExtension.GetCertsUIAuthorizationData</c> when needed.
  /// </summary>
  protected Result RBACWrapper(
      CertsUIAuthorizationData certsAuthorizationData,
      Func<JwtTokenData, Result>? userRules,
      Func<ApiKeyData, Result>? apiKeyRules) {
    if (certsAuthorizationData.IsJwtAuthorization) {
      var jwtTokenData = certsAuthorizationData.JwtTokenData;
      return RBACWrapperJwtToken(jwtTokenData!, userRules);
    }

    if (certsAuthorizationData.IsApiKeyAuthorization) {
      var apiKeyData = certsAuthorizationData.ApiKeyData;
      return RBACWrapperApiKey(apiKeyData!, apiKeyRules);
    }

    return NoValidAuthorizationMethod();
  }

  protected Result RBACWrapperJwtToken(JwtTokenData jwtTokenData, Func<JwtTokenData, Result>? userRules) {
    if (jwtTokenData.IsGlobalAdmin) {
      _logger.LogInformation($"Admin access granted for user {jwtTokenData.UserId}.");
      return Result.Ok();
    }

    if (userRules != null) {
      return userRules(jwtTokenData);
    }

    _logger.LogInformation($"Access denied: User {jwtTokenData.UserId} does not have access to resource.");
    return Result.Forbidden("User does not have access to resource.");
  }

  protected Result RBACWrapperApiKey(ApiKeyData apiKeyData, Func<ApiKeyData, Result>? apiKeyRules) {
    if (apiKeyData.IsGlobalAdmin) {
      _logger.LogInformation($"Admin access granted via API key {apiKeyData.ApiKeyId}.");
      return Result.Ok();
    }
    if (apiKeyRules != null) {
      return apiKeyRules(apiKeyData);
    }
    _logger.LogInformation($"Access denied: API key {apiKeyData.ApiKeyId} does not have access to resource.");
    return Result.Forbidden("ApiKey does not have access to resource.");
  }

  /// <summary>Generic resource-carrying RBAC (Vault parallel).</summary>
  protected Result<T?> RBACWrapper<T>(
      CertsUIAuthorizationData certsAuthorizationData,
      T resource,
      Func<T, Result<T?>>? userRules,
      Func<T, Result<T?>>? apiKeyRules) {
    if (certsAuthorizationData.IsJwtAuthorization) {
      var jwtTokenData = certsAuthorizationData.JwtTokenData;
      return RBACWrapperJwtToken(jwtTokenData!, resource, userRules);
    }

    if (certsAuthorizationData.IsApiKeyAuthorization) {
      var apiKeyData = certsAuthorizationData.ApiKeyData;
      return RBACWrapperApiKey(apiKeyData!, resource, apiKeyRules);
    }

    return NoValidAuthorizationMethod<T>();
  }

  protected Result<T?> RBACWrapperJwtToken<T>(
      JwtTokenData jwtTokenData,
      T resource,
      Func<T, Result<T?>>? userRules) {
    if (jwtTokenData.IsGlobalAdmin) {
      _logger.LogInformation($"Admin access granted for user {jwtTokenData.UserId}.");
      return Result<T?>.Ok(resource);
    }

    if (userRules != null) {
      return userRules(resource);
    }

    _logger.LogInformation($"Access denied: User {jwtTokenData.UserId} does not have access to resources.");
    return Result<T?>.Forbidden(default, "User does not have access to resources.");
  }

  protected Result<T?> RBACWrapperApiKey<T>(
      ApiKeyData apiKeyData,
      T resource,
      Func<T, Result<T?>>? apiKeyRules) {
    if (apiKeyData.IsGlobalAdmin) {
      _logger.LogInformation($"Admin access granted via API key {apiKeyData.ApiKeyId}.");
      return Result<T?>.Ok(resource);
    }

    if (apiKeyRules != null) {
      return apiKeyRules(resource);
    }

    _logger.LogInformation($"Access denied: API key {apiKeyData.ApiKeyId} does not have access to resources.");
    return Result<T?>.Forbidden(default, "ApiKey does not have access to resources.");
  }
}

/// <summary>
/// Maps domain and query read models to API contracts (combined with non-generic helpers in same file).
/// </summary>
public abstract class ServiceBase<TResponse, TDomain, TSearchResponse, TQueryResult>(
  ILogger logger,
  IOptions<Configuration> appSettings
) : ServiceBase(logger, appSettings) {

  protected abstract TResponse MapToResponse(TDomain domain);

  protected abstract TSearchResponse MapToSearchResponse(TQueryResult queryResult);
}
