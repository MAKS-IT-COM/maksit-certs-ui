using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

public interface IApiKeyPersistenceService {
  #region Read
  Result<ApiKey?> ReadById(Guid apiKeyId);
  Result<ApiKey?> ReadAPIKey(string apiKeyValue);
  #endregion

  #region Write
  Result<ApiKey?> Write(ApiKey apiKey);
  Result<ApiKey?> Write(ApiKey apiKey, ApiKeyAuthorization? authorization);
  Result<List<ApiKey>?> WriteMany(List<ApiKey> apiKeys);
  #endregion

  #region Delete
  Result DeleteById(Guid apiKeyId);
  Result DeleteMany(List<Guid> apiKeyIds);
  #endregion
}
