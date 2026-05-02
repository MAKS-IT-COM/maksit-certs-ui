using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

public interface IApiKeyAuthorizationPersistenceService
{
  #region Read
  Result<ApiKeyAuthorization?> ReadByApiKeyId(Guid apiKeyId);
  #endregion

  #region Write
  Result Write(ApiKeyAuthorization authorization);
  #endregion
}
