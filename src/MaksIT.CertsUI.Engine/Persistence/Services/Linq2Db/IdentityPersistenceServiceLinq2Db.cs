using System.Linq.Expressions;
using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Mappers;
using Microsoft.Extensions.Logging;

namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IIdentityPersistenceService"/>.
/// </summary>
public class IdentityPersistenceServiceLinq2Db(ILogger<IdentityPersistenceServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory, UserMapper userMapper) : IIdentityPersistenceService {
  private readonly ILogger<IdentityPersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;
  private readonly UserMapper _userMapper = userMapper;

  public Result<User?> ReadById(Guid userId) =>
    GetSingleUser(db => db.GetTable<UserDto>().Where(u => u.Id == userId), $"ID {userId}", $"User with ID {userId} not found.");

  public Result<User?> ReadByUsername(string username) {
    var normalized = (username?.Trim() ?? "").ToLowerInvariant();

    return GetSingleUser(
      db => db.GetTable<UserDto>()
        .Where(u =>
          u.Username != null &&
          u.Username.ToLower() == normalized
        ), $"username {username}", $"User with username {username} not found.");

  }

  public Result<User?> ReadByEmail(string email) =>
    GetSingleUser(db => db.GetTable<UserDto>().Where(u => u.Email == email), $"email {email}", $"User with email {email} not found.");

  public Result<User?> ReadByToken(string token) {
    using var db = _connectionFactory.Create();

    var userId = db.GetTable<JwtTokenDto>()
      .Where(t => t.Token == token)
      .Select(t => t.UserId)
      .FirstOrDefault();

    if (userId == default)
      return Result<User?>.NotFound(null, $"User with token {token} not found.");

    return ReadById(userId);
  }

  public Result<User?> ReadByRefreshToken(string refreshToken) {
    using var db = _connectionFactory.Create();

    var userId = db.GetTable<JwtTokenDto>()
      .Where(t => t.RefreshToken == refreshToken)
      .Select(t => t.UserId)
      .FirstOrDefault();

    if (userId == default)
      return Result<User?>.NotFound(null, $"User with refresh token {refreshToken} not found.");

    return ReadById(userId);
  }

  private Result<User?> GetSingleUser(Func<LinqToDB.Data.DataConnection, IQueryable<UserDto>> queryFn, string identifier, string notFoundMessage) {
    try {
      using var db = _connectionFactory.Create();

      var userDto = queryFn(db).FirstOrDefault();

      if (userDto == null)
        return Result<User?>.NotFound(null, notFoundMessage);

      LoadUserChildren(db, userDto);

      var user = _userMapper.MapToDomain(userDto);
      return Result<User?>.Ok(user);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading user with {Identifier}", identifier);
      return Result<User?>.InternalServerError(null, ["An error occurred while retrieving the user.", .. ex.ExtractMessages()]);
    }
  }

  private static void LoadUserChildren(LinqToDB.Data.DataConnection db, UserDto userDto) {
    userDto.JwtTokens = [.. db.GetTable<JwtTokenDto>()
      .Where(t => t.UserId == userDto.Id)];

    userDto.TwoFactorRecoveryCodes = [.. db.GetTable<TwoFactorRecoveryCodeDto>()
      .Where(t => t.UserId == userDto.Id)];

    userDto.EntityScopes = [.. db.GetTable<UserEntityScopeDto>()
      .Where(s => s.UserId == userDto.Id)];
  }

  public Result<User?> Write(User user, UserAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(user);

    try {
      var dto = UserMapper.MapToDto(user);
      UserMapper.ApplyAuthorizationToDto(dto, user.Id, authorization);

      using var db = _connectionFactory.Create();
      var existing = db.GetTable<UserDto>().Where(u => u.Id == user.Id).FirstOrDefault();
      if (existing != null) {
        LoadUserChildren(db, existing);
        UpdateUserRow(db, dto);
        SyncJwtTokens(db, user.Id, existing.JwtTokens, dto.JwtTokens);
        SyncTwoFactorRecoveryCodes(db, user.Id, existing.TwoFactorRecoveryCodes, dto.TwoFactorRecoveryCodes);
        SyncUserEntityScopes(db, user.Id, existing.EntityScopes, dto.EntityScopes);
      }
      else {
        db.Insert(dto);
        foreach (var t in dto.JwtTokens) db.Insert(t);
        foreach (var r in dto.TwoFactorRecoveryCodes) db.Insert(r);
        foreach (var s in dto.EntityScopes) db.Insert(s);
      }
      return Result<User?>.Ok(user);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error writing user {UserId}", user.Id);
      return Result<User?>.InternalServerError(null, ["An error occurred while saving the user.", .. ex.ExtractMessages()]);
    }
  }

  private static void UpdateUserRow(LinqToDB.Data.DataConnection db, UserDto dto) {
    db.GetTable<UserDto>()
      .Where(u => u.Id == dto.Id)
      .Set(u => u.Username, dto.Username)
      .Set(u => u.Email, dto.Email)
      .Set(u => u.MobileNumber, dto.MobileNumber)
      .Set(u => u.IsActive, dto.IsActive)
      .Set(u => u.IsGlobalAdmin, dto.IsGlobalAdmin)
      .Set(u => u.PasswordSalt, dto.PasswordSalt)
      .Set(u => u.PasswordHash, dto.PasswordHash)
      .Set(u => u.TwoFactorSharedKey, dto.TwoFactorSharedKey)
      .Set(u => u.CreatedAt, dto.CreatedAt)
      .Set(u => u.LastLogin, dto.LastLogin)
      .Update();
  }

  private static void SyncJwtTokens(LinqToDB.Data.DataConnection db, Guid _, List<JwtTokenDto> existing, List<JwtTokenDto> desired) {
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
          .Update();
      else
        db.Insert(d);
    }
  }

  private static void SyncTwoFactorRecoveryCodes(LinqToDB.Data.DataConnection db, Guid _, List<TwoFactorRecoveryCodeDto> existing, List<TwoFactorRecoveryCodeDto> desired) {
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

  private static void SyncUserEntityScopes(LinqToDB.Data.DataConnection db, Guid userId, List<UserEntityScopeDto> existing, List<UserEntityScopeDto> desired) {
    var desiredKeys = desired.Select(s => (s.EntityId, s.EntityType, s.Scope)).ToHashSet();

    foreach (var e in existing.Where(s => !desiredKeys.Contains((s.EntityId, s.EntityType, s.Scope))))
      db.GetTable<UserEntityScopeDto>().Where(s => s.Id == e.Id).Delete();

    foreach (var d in desired) {
      var match = existing.FirstOrDefault(x => x.EntityId == d.EntityId && x.EntityType == d.EntityType && x.Scope == d.Scope);
      if (match != null) {
        if (match.Id != d.Id) {
          db.GetTable<UserEntityScopeDto>().Where(s => s.Id == match.Id).Delete();
          d.UserId = userId;
          db.Insert(d);
        }
        else {
          db.GetTable<UserEntityScopeDto>().Where(s => s.Id == d.Id)
            .Set(s => s.EntityId, d.EntityId)
            .Set(s => s.EntityType, d.EntityType)
            .Set(s => s.Scope, d.Scope)
            .Update();
        }
      }
      else {
        d.UserId = userId;
        db.Insert(d);
      }
    }
  }

  public Result<List<User>?> WriteMany(List<User> users) {
    ArgumentNullException.ThrowIfNull(users);

    if (users.Count == 0)
      return Result<List<User>?>.Ok(users);

    foreach (var user in users) {
      var result = Write(user, (UserAuthorization?)null);
      if (!result.IsSuccess)
        return result.ToResultOfType<List<User>?>(_ => (List<User>?)null);
    }

    return Result<List<User>?>.Ok(users);
  }

  public Result DeleteById(Guid userId) => DeleteMany([userId]);

  public Result DeleteMany(List<Guid> userIds) {
    ArgumentNullException.ThrowIfNull(userIds);

    try {
      using var db = _connectionFactory.Create();

      foreach (var userId in userIds) {
        var exists = db.GetTable<UserDto>().Any(u => u.Id == userId);

        if (!exists)
          return Result.NotFound($"User with ID {userId} not found.");

        db.GetTable<UserEntityScopeDto>().Where(s => s.UserId == userId).Delete();
        db.GetTable<TwoFactorRecoveryCodeDto>().Where(t => t.UserId == userId).Delete();
        db.GetTable<JwtTokenDto>().Where(t => t.UserId == userId).Delete();
        db.GetTable<UserDto>().Where(u => u.Id == userId).Delete();
      }

      return Result.Ok();
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting users");

      return Result.InternalServerError(["Error occurred while deleting users.", .. ex.ExtractMessages()]);
    }
  }
}
