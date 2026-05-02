using Microsoft.Extensions.Logging;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Persistence.Services;
using MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;


namespace MaksIT.CertsUI.Engine.DomainServices;

public interface IApiKeyDomainService {
  #region Read
  Result<ApiKey?> ReadAPIKey(Guid apiKeyId);
  Result<ApiKey?> ReadAPIKey(string apiKeyValue);
  Result<ApiKeyAuthorization?> ReadApiKeyAuthorization(Guid apiKeyId);
  #endregion

  #region Write
  Task<Result<ApiKey?>> WriteAPIKeyAsync(ApiKey apiKey);
  Task<Result<ApiKey?>> WriteAPIKeyAsync(ApiKey apiKey, ApiKeyAuthorization? authorization);
  Task<Result> WriteApiKeyAuthorizationAsync(ApiKeyAuthorization authorization);
  #endregion

  #region Delete
  Task<Result> DeleteAPIKeyAsync(Guid apiKeyId);
  #endregion
}

public class ApiKeyDomainService(
  ILogger<ApiKeyDomainService> logger,
  IApiKeyPersistenceService apiKeyPersistenceSevice,
  IApiKeyAuthorizationPersistenceService apiKeyAuthorizationPersistenceService
) : IApiKeyDomainService {

  private readonly ILogger<ApiKeyDomainService> _logger = logger;
  private readonly IApiKeyPersistenceService _apiKeyPersistenceSevice = apiKeyPersistenceSevice;
  private readonly IApiKeyAuthorizationPersistenceService _apiKeyAuthorizationPersistenceService = apiKeyAuthorizationPersistenceService;

  #region Read
  public Result<ApiKey?> ReadAPIKey(Guid apiKeyId) =>
    _apiKeyPersistenceSevice.ReadById(apiKeyId);

  public Result<ApiKey?> ReadAPIKey(string apiKeyValue) =>
    _apiKeyPersistenceSevice.ReadAPIKey(apiKeyValue);

  public Result<ApiKeyAuthorization?> ReadApiKeyAuthorization(Guid apiKeyId) =>
    _apiKeyAuthorizationPersistenceService.ReadByApiKeyId(apiKeyId);
  #endregion

  #region Write
  /// <summary>Persists API key and optional authorization. When authorization is null, loads current authorization so auth is never overwritten.</summary>
  public Task<Result<ApiKey?>> WriteAPIKeyAsync(ApiKey apiKey) {
    var authResult = _apiKeyAuthorizationPersistenceService.ReadByApiKeyId(apiKey.Id);
    return WriteAPIKeyAsync(apiKey, authResult.IsSuccess ? authResult.Value : null);
  }

  public Task<Result<ApiKey?>> WriteAPIKeyAsync(ApiKey apiKey, ApiKeyAuthorization? authorization) =>
    Task.FromResult(_apiKeyPersistenceSevice.Write(apiKey, authorization));

  public Task<Result> WriteApiKeyAuthorizationAsync(ApiKeyAuthorization authorization) =>
    Task.FromResult(_apiKeyAuthorizationPersistenceService.Write(authorization));
  #endregion

  #region Delete
  public Task<Result> DeleteAPIKeyAsync(Guid apiKeyId) =>
    Task.FromResult(_apiKeyPersistenceSevice.DeleteById(apiKeyId));
  #endregion
}
