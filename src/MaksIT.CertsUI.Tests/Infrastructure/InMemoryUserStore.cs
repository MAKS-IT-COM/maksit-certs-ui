using System.Linq;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.Query;
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

  public Task<Result<PagedQueryResult<UserQueryResult>>> SearchUsersAsync(
    string? usernameFilter,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default) {
    lock (_lock) {
      IEnumerable<User> filtered = _users;
      if (!string.IsNullOrWhiteSpace(usernameFilter))
        filtered = filtered.Where(u => u.Username.Contains(usernameFilter, StringComparison.OrdinalIgnoreCase));
      var ordered = filtered.OrderBy(u => u.Username).ToList();
      var page = Math.Max(1, pageNumber);
      var size = Math.Clamp(pageSize, 1, 500);
      var total = ordered.Count;
      var slice = ordered.Skip((page - 1) * size).Take(size).ToList();
      var data = slice.Select(u => new UserQueryResult {
        Id = u.Id,
        Username = u.Username,
        IsActive = u.IsActive,
        TwoFactorEnabled = u.TwoFactorEnabled,
        LastLogin = u.LastLogin,
      }).ToList();
      return Task.FromResult(Result<PagedQueryResult<UserQueryResult>>.Ok(new PagedQueryResult<UserQueryResult>(
        data,
        total,
        page,
        size
      )));
    }
  }

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
