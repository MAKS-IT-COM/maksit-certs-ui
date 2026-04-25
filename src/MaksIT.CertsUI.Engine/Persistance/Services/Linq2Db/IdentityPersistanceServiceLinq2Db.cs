using LinqToDB;
using LinqToDB.Data;
using MaksIT.Core.Extensions;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistance.Mappers;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.Results;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.Persistance.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IIdentityPersistanceService"/>.
/// </summary>
public sealed class IdentityPersistanceServiceLinq2Db(
  ILogger<IdentityPersistanceServiceLinq2Db> logger,
  ICertsDataConnectionFactory connectionFactory,
  UserMapper userMapper
) : IIdentityPersistanceService {

  private readonly ILogger<IdentityPersistanceServiceLinq2Db> _logger = logger;
  private readonly ICertsDataConnectionFactory _connectionFactory = connectionFactory;
  private readonly UserMapper _userMapper = userMapper;

  public Task<int> CountAsync(CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    using var db = _connectionFactory.Create();
    return Task.FromResult(db.GetTable<UserDto>().Count());
  }

  public Task<Result<List<User>>> GetAllUsersAsync(CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var rows = db.GetTable<UserDto>().ToList();
      if (rows.Count == 0)
        return Task.FromResult(Result<List<User>>.Ok([]));

      var userIds = rows.Select(r => r.Id).ToList();
      var allTokens = db.GetTable<JwtTokenDto>().Where(t => userIds.Contains(t.UserId)).ToList();
      var allRecovery = db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => userIds.Contains(t.UserId)).ToList();
      foreach (var row in rows) {
        row.JwtTokens = [.. allTokens.Where(t => t.UserId == row.Id)];
        row.TwoFactorRecoveryCodes = [.. allRecovery.Where(t => t.UserId == row.Id)];
      }

      return Task.FromResult(Result<List<User>>.Ok([.. rows.Select(r => _userMapper.MapToDomain(r))]));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error listing users.");
      return Task.FromResult(Result<List<User>>.InternalServerError([], ["An error occurred while listing users.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<User?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    return GetSingleUserAsync(
      db => db.GetTable<UserDto>().FirstOrDefault(u => u.Id == id),
      $"ID {id}",
      "User not found.");
  }

  public Task<Result<User?>> GetByNameAsync(string name, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    return GetSingleUserAsync(
      db => db.GetTable<UserDto>().FirstOrDefault(u => u.Name == name),
      $"name {name}",
      "User not found.");
  }

  public Task<Result<User?>> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var userId = db.GetTable<JwtTokenDto>()
        .Where(t => t.RefreshToken == refreshToken)
        .Select(t => t.UserId)
        .FirstOrDefault();

      if (userId == default)
        return Task.FromResult(Result<User?>.NotFound(null, "User not found for the provided refresh token."));

      return GetByIdAsync(userId, cancellationToken);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading user by refresh token.");
      return Task.FromResult(Result<User?>.InternalServerError(null, ["An error occurred while retrieving the user.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<User?>> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var userId = db.GetTable<JwtTokenDto>()
        .Where(t => t.Token == accessToken)
        .Select(t => t.UserId)
        .FirstOrDefault();

      if (userId == default)
        return Task.FromResult(Result<User?>.NotFound(null, "User not found."));

      return GetByIdAsync(userId, cancellationToken);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading user by access token.");
      return Task.FromResult(Result<User?>.InternalServerError(null, ["An error occurred while retrieving the user.", .. ex.ExtractMessages()]));
    }
  }

  private Task<Result<User?>> GetSingleUserAsync(Func<DataConnection, UserDto?> queryFn, string identifier, string notFoundMessage) {
    try {
      using var db = _connectionFactory.Create();
      var row = queryFn(db);
      if (row == null)
        return Task.FromResult(Result<User?>.NotFound(null, notFoundMessage));
      LoadUserChildren(db, row);
      return Task.FromResult(Result<User?>.Ok(_userMapper.MapToDomain(row)));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading user with {Identifier}", identifier);
      return Task.FromResult(Result<User?>.InternalServerError(null, ["An error occurred while retrieving the user.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> UpsertUserAsync(User user, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    ArgumentNullException.ThrowIfNull(user);

    try {
      using var db = _connectionFactory.Create();
      var dto = UserMapper.MapToDto(user);
      var existing = db.GetTable<UserDto>().FirstOrDefault(u => u.Id == user.Id);
      if (existing != null) {
        LoadUserChildren(db, existing);
        UpdateUserRow(db, dto);
        SyncJwtTokens(db, user.Id, existing.JwtTokens, dto.JwtTokens);
        SyncTwoFactorRecoveryCodes(db, user.Id, existing.TwoFactorRecoveryCodes, dto.TwoFactorRecoveryCodes);
      }
      else {
        db.Insert(dto);
        foreach (var t in dto.JwtTokens)
          db.Insert(t);
        foreach (var r in dto.TwoFactorRecoveryCodes)
          db.Insert(r);
      }
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error writing user {UserId}", user.Id);
      return Task.FromResult(Result.InternalServerError(["An error occurred while saving the user.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      var exists = db.GetTable<UserDto>().Any(u => u.Id == id);
      if (!exists)
        return Task.FromResult(Result.NotFound("User not found."));
      db.GetTable<JwtTokenDto>().Where(t => t.UserId == id).Delete();
      db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => t.UserId == id).Delete();
      db.GetTable<UserDto>().Where(u => u.Id == id).Delete();
      _logger.LogInformation("Deleted user {UserId}.", id);
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting user {UserId}", id);
      return Task.FromResult(Result.InternalServerError(["An error occurred while deleting the user.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result<User?>> CreateUserWithPasswordAsync(string username, string password, string pepper, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      if (db.GetTable<UserDto>().Any(u => u.Name == username))
        return Task.FromResult(Result<User?>.BadRequest(null, $"User name '{username}' is already taken."));
      User user;
      try {
        user = new User(username, password, pepper);
      }
      catch (InvalidOperationException ex) {
        return Task.FromResult(Result<User?>.InternalServerError(null, [ex.Message]));
      }
      var rowDto = UserMapper.MapToDto(user);
      db.Insert(rowDto);
      return Task.FromResult(Result<User?>.Ok(user));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error creating user {Username}", username);
      return Task.FromResult(Result<User?>.InternalServerError(null, ["An error occurred while creating the user.", .. ex.ExtractMessages()]));
    }
  }

  public Task<Result> EnsureDefaultAdminAsync(string pepper, string defaultUsername, string defaultPassword, CancellationToken cancellationToken = default) {
    cancellationToken.ThrowIfCancellationRequested();
    try {
      using var db = _connectionFactory.Create();
      if (db.GetTable<UserDto>().Any())
        return Task.FromResult(Result.Ok());
      User admin;
      try {
        admin = new User(defaultUsername.Trim(), defaultPassword, pepper);
      }
      catch (InvalidOperationException ex) {
        return Task.FromResult(Result.InternalServerError([ex.Message]));
      }
      db.Insert(UserMapper.MapToDto(admin));
      _logger.LogInformation("Created default admin user.");
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error ensuring default admin user.");
      return Task.FromResult(Result.InternalServerError(["An error occurred while ensuring the default admin user.", .. ex.ExtractMessages()]));
    }
  }

  private static void LoadUserChildren(DataConnection db, UserDto userDto) {
    userDto.JwtTokens = [.. db.GetTable<JwtTokenDto>().Where(t => t.UserId == userDto.Id)];
    userDto.TwoFactorRecoveryCodes = [.. db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => t.UserId == userDto.Id)];
  }

  private static void UpdateUserRow(DataConnection db, UserDto dto) {
    db.GetTable<UserDto>()
      .Where(u => u.Id == dto.Id)
      .Set(u => u.Name, dto.Name)
      .Set(u => u.Salt, dto.Salt)
      .Set(u => u.Hash, dto.Hash)
      .Set(u => u.LastLoginUtc, dto.LastLoginUtc)
      .Set(u => u.IsActive, dto.IsActive)
      .Set(u => u.TwoFactorSharedKey, dto.TwoFactorSharedKey)
      .Update();
  }

  private static void SyncJwtTokens(DataConnection db, Guid userId, List<JwtTokenDto> existing, List<JwtTokenDto> desired) {
    foreach (var d in desired)
      d.UserId = userId;

    var desiredIds = desired.Select(t => t.Id).ToHashSet();

    foreach (var e in existing.Where(t => !desiredIds.Contains(t.Id)))
      db.GetTable<JwtTokenDto>().Where(t => t.Id == e.Id).Delete();

    foreach (var d in desired) {
      if (existing.Any(t => t.Id == d.Id))
        db.GetTable<JwtTokenDto>().Where(t => t.Id == d.Id)
          .Set(t => t.Token, d.Token)
          .Set(t => t.RefreshToken, d.RefreshToken)
          .Set(t => t.IssuedAt, d.IssuedAt)
          .Set(t => t.ExpiresAt, d.ExpiresAt)
          .Set(t => t.RefreshTokenExpiresAt, d.RefreshTokenExpiresAt)
          .Set(t => t.IsRevoked, d.IsRevoked)
          .Set(t => t.UserId, d.UserId)
          .Update();
      else
        db.Insert(d);
    }
  }

  private static void SyncTwoFactorRecoveryCodes(DataConnection db, Guid _, List<TwoFactorRecoveryCodeDto> existing, List<TwoFactorRecoveryCodeDto> desired) {
    var desiredIds = desired.Select(t => t.Id).ToHashSet();

    foreach (var e in existing.Where(t => !desiredIds.Contains(t.Id)))
      db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => t.Id == e.Id).Delete();

    foreach (var d in desired) {
      if (!existing.Any(t => t.Id == d.Id))
        db.Insert(d);
      else
        db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => t.Id == d.Id)
          .Set(t => t.Salt, d.Salt)
          .Set(t => t.Hash, d.Hash)
          .Set(t => t.IsUsed, d.IsUsed)
          .Update();
    }
  }
}
