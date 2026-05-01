using System.Linq;
using System.Linq.Expressions;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine.QueryServices.Identity;
using MaksIT.Results;
using MaksIT.CertsUI.Services;

namespace MaksIT.CertsUI.Tests.Infrastructure;

/// <summary>In-memory user persistence + query store for unit tests (no PostgreSQL).</summary>
public sealed class InMemoryUserStore : IIdentityPersistanceService, IUserQueryService {

  readonly List<User> _users = [];
  readonly object _lock = new();

  public Task<int> CountAsync(CancellationToken cancellationToken = default) {
    lock (_lock)
      return Task.FromResult(_users.Count);
  }

  public Task<Result<List<User>>> GetAllUsersAsync(CancellationToken cancellationToken = default) {
    lock (_lock)
      return Task.FromResult(Result<List<User>>.Ok([.. _users]));
  }

  public Result<int?> Count(Expression<Func<UserDto, bool>>? usersPredicate) {
    lock (_lock) {
      var q = _users.Select(ToUserDto).AsQueryable();
      if (usersPredicate != null)
        q = q.Where(usersPredicate);
      return Result<int?>.Ok(q.Count());
    }
  }

  public Result<List<UserQueryResult>?> Search(
    Expression<Func<UserDto, bool>>? usersPredicate,
    int? skip,
    int? limit) {
    lock (_lock) {
      var q = _users.Select(ToUserDto).AsQueryable();
      if (usersPredicate != null)
        q = q.Where(usersPredicate);
      q = q.OrderBy(x => x.Name);
      if (skip.HasValue)
        q = q.Skip(skip.Value);
      if (limit.HasValue)
        q = q.Take(limit.Value);
      var dtos = q.ToList();
      var results = dtos.Select(d => MapToQueryResult(d, d.TwoFactorRecoveryCodes.Count)).ToList();
      return Result<List<UserQueryResult>?>.Ok(results);
    }
  }

  private static UserDto ToUserDto(User u) => new() {
    Id = u.Id,
    Name = u.Username,
    Salt = u.PasswordSalt,
    Hash = u.PasswordHash,
    LastLoginUtc = u.LastLogin ?? default,
    IsActive = u.IsActive,
    TwoFactorSharedKey = u.TwoFactorSharedKey,
    JwtTokens = [],
    TwoFactorRecoveryCodes = [.. u.TwoFactorRecoveryCodes.Select(rc => new TwoFactorRecoveryCodeDto {
      Id = rc.Id,
      UserId = u.Id,
      Salt = rc.Salt,
      Hash = rc.Hash,
      IsUsed = rc.IsUsed
    })]
  };

  private static UserQueryResult MapToQueryResult(UserDto row, int recoveryCount) => new() {
    Id = row.Id,
    Username = row.Name,
    IsActive = row.IsActive,
    TwoFactorEnabled = row.TwoFactorSharedKey != null && recoveryCount > 0,
    LastLogin = row.LastLoginUtc == default ? null : row.LastLoginUtc,
  };

  public Task<Result<User?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    lock (_lock) {
      var u = _users.FirstOrDefault(x => x.Id == id);
      return Task.FromResult(u == null ? Result<User?>.NotFound(null, "User not found.") : Result<User?>.Ok(u));
    }
  }

  public Task<Result<User?>> GetByNameAsync(string name, CancellationToken cancellationToken = default) {
    lock (_lock) {
      var u = _users.FirstOrDefault(x => x.Username == name);
      return Task.FromResult(u == null ? Result<User?>.NotFound(null, "User not found.") : Result<User?>.Ok(u));
    }
  }

  public Task<Result<User?>> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
    lock (_lock) {
      foreach (var u in _users) {
        if (u.Tokens.Any(t => t.RefreshToken == refreshToken))
          return Task.FromResult(Result<User?>.Ok(u));
      }
      return Task.FromResult(Result<User?>.NotFound(null, "User not found for the provided refresh token."));
    }
  }

  public Task<Result<User?>> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) {
    lock (_lock) {
      foreach (var u in _users) {
        if (u.Tokens.Any(t => t.Token == accessToken))
          return Task.FromResult(Result<User?>.Ok(u));
      }
      return Task.FromResult(Result<User?>.NotFound(null, "User not found."));
    }
  }

  public Task<Result> UpsertUserAsync(User user, CancellationToken cancellationToken = default) {
    lock (_lock) {
      var existing = _users.FirstOrDefault(u => u.Id == user.Id);
      if (existing != null)
        _users.Remove(existing);
      _users.Add(user);
    }
    return Task.FromResult(Result.Ok());
  }

  public Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default) {
    lock (_lock) {
      var u = _users.FirstOrDefault(x => x.Id == id);
      if (u == null)
        return Task.FromResult(Result.NotFound("User not found."));
      _users.Remove(u);
      return Task.FromResult(Result.Ok());
    }
  }

  public Task<Result<User?>> CreateUserWithPasswordAsync(string username, string password, string pepper, CancellationToken cancellationToken = default) {
    User user;
    try {
      user = new User(username, password, pepper);
    }
    catch (InvalidOperationException ex) {
      return Task.FromResult(Result<User?>.InternalServerError(null, [ex.Message]));
    }
    lock (_lock) {
      if (_users.Any(x => x.Username == username))
        return Task.FromResult(Result<User?>.BadRequest(null, $"User name '{username}' is already taken."));
      _users.Add(user);
    }
    return Task.FromResult(Result<User?>.Ok(user));
  }

  public Task<Result> EnsureDefaultAdminAsync(string pepper, string defaultUsername, string defaultPassword, CancellationToken cancellationToken = default) {
    lock (_lock) {
      if (_users.Count > 0)
        return Task.FromResult(Result.Ok());
    }
    User admin;
    try {
      admin = new User(defaultUsername.Trim(), defaultPassword, pepper);
    }
    catch (InvalidOperationException ex) {
      return Task.FromResult(Result.InternalServerError([ex.Message]));
    }
    lock (_lock) {
      if (_users.Any(x => x.Username == admin.Username))
        return Task.FromResult(Result.Ok());
      _users.Add(admin);
    }
    return Task.FromResult(Result.Ok());
  }
}
