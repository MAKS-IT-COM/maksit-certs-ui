using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.Results;

namespace MaksIT.CertsUI.Engine.Persistance.Services;

/// <summary>
/// User and JWT token persistence. Certs identity model: users, <c>jwt_tokens</c>, and optional 2FA recovery rows.
/// </summary>
public interface IIdentityPersistanceService {

  #region Read

  Task<int> CountAsync(CancellationToken cancellationToken = default);

  Task<Result<List<User>>> GetAllUsersAsync(CancellationToken cancellationToken = default);

  Task<Result<User?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task<Result<User?>> GetByNameAsync(string name, CancellationToken cancellationToken = default);

  Task<Result<User?>> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

  Task<Result<User?>> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

  #endregion

  #region Write

  Task<Result> UpsertUserAsync(User user, CancellationToken cancellationToken = default);

  Task<Result<User?>> CreateUserWithPasswordAsync(string username, string password, string pepper, CancellationToken cancellationToken = default);

  Task<Result> EnsureDefaultAdminAsync(string pepper, string defaultUsername, string defaultPassword, CancellationToken cancellationToken = default);

  #endregion

  #region Delete

  Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

  #endregion
}
