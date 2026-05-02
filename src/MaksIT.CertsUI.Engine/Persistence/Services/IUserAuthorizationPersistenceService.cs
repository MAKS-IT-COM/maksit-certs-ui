using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

public interface IUserAuthorizationPersistenceService {
  #region Read
  Result<UserAuthorization?> ReadByUserId(Guid userId);

  Result<List<Guid>?> ReadGlobalAdminUserIds();
  #endregion

  #region Write
  Result Write(UserAuthorization authorization);
  #endregion
}
