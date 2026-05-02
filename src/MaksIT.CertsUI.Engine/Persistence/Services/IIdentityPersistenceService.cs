using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Services;

public interface IIdentityPersistenceService {
  #region Read
  Result<User?> ReadById(Guid userId);
  Result<User?> ReadByUsername(string username);
  Result<User?> ReadByEmail(string email);
  Result<User?> ReadByToken(string token);
  Result<User?> ReadByRefreshToken(string refreshToken);
  #endregion

  #region Write
  Result<User?> Write(User user, UserAuthorization? authorization = null);
  Result<List<User>?> WriteMany(List<User> users);
  #endregion

  #region Delete
  Result DeleteById(Guid userId);
  Result DeleteMany(List<Guid> usersIds);
  #endregion
}
