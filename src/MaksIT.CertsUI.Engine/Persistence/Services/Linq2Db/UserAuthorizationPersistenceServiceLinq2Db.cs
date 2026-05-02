using Microsoft.Extensions.Logging;
using LinqToDB;
using MaksIT.Core.Extensions;
using MaksIT.Results;
using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.Persistence.Mappers;


namespace MaksIT.CertsUI.Engine.Persistence.Services.Linq2Db;

/// <summary>
/// Linq2Db-based implementation of <see cref="IUserAuthorizationPersistenceService"/>.
/// </summary>
public class UserAuthorizationPersistenceServiceLinq2Db(ILogger<UserAuthorizationPersistenceServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IUserAuthorizationPersistenceService {
  private readonly ILogger<UserAuthorizationPersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<UserAuthorization?> ReadByUserId(Guid userId) {
    try {
      using var db = _connectionFactory.Create();

      var userDto = db.GetTable<UserDto>()
        .Where(u => u.Id == userId)
        .FirstOrDefault();

      if (userDto == null)
        return Result<UserAuthorization?>.NotFound(null, $"User {userId} not found.");

      userDto.EntityScopes = [.. db.GetTable<UserEntityScopeDto>()
        .Where(s => s.UserId == userId)];

      var authorization = UserMapper.ToAuthorization(userDto);

      return Result<UserAuthorization?>.Ok(authorization);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading authorization for user {UserId}", userId);
      return Result<UserAuthorization?>.InternalServerError(null, ["An error occurred while retrieving user authorization.", .. ex.ExtractMessages()]);
    }
  }

  public Result Write(UserAuthorization authorization) {
    if (authorization == null) return Result.BadRequest("Authorization is null.");

    try {
      using var db = _connectionFactory.Create();

      var userDto = db.GetTable<UserDto>()
        .Where(u => u.Id == authorization.UserId)
        .FirstOrDefault();

      if (userDto == null)
        return Result.NotFound($"User {authorization.UserId} not found.");

      db.GetTable<UserDto>().Where(u => u.Id == authorization.UserId)
        .Set(u => u.IsGlobalAdmin, authorization.IsGlobalAdmin)
        .Update();

      var existing = db.GetTable<UserEntityScopeDto>()
        .Where(s => s.UserId == authorization.UserId)
        .ToList();

      var desired = UserMapper.ToEntityScopeDtos(authorization.EntityScopes, authorization.UserId);

      var desiredKeys = desired.Select(s => (s.EntityId, s.EntityType, s.Scope)).ToHashSet();

      foreach (var e in existing.Where(s => !desiredKeys.Contains((s.EntityId, s.EntityType, s.Scope))))
        db.GetTable<UserEntityScopeDto>()
        .Where(s => s.Id == e.Id)
        .Delete();

      foreach (var d in desired) {
        var match = existing.FirstOrDefault(x => x.EntityId == d.EntityId && x.EntityType == d.EntityType && x.Scope == d.Scope);

        if (match == null) {
          d.UserId = authorization.UserId;
          db.Insert(d);
        }
        else if (match.Id != d.Id) {
          db.GetTable<UserEntityScopeDto>()
            .Where(s => s.Id == match.Id)
            .Delete();

          d.UserId = authorization.UserId;
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

      return Result.Ok();
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error writing authorization for user {UserId}", authorization.UserId);
      return Result.InternalServerError(["An error occurred while saving user authorization.", .. ex.ExtractMessages()]);
    }
  }

  public Result<List<Guid>?> ReadGlobalAdminUserIds() {
    try {
      using var db = _connectionFactory.Create();

      var ids = db.GetTable<UserDto>()
        .Where(x => x.IsGlobalAdmin)
        .Select(x => x.Id)
        .ToList();

      return Result<List<Guid>?>.Ok(ids);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading global admin user IDs");

      return Result<List<Guid>?>.InternalServerError(null, ["An error occurred while retrieving global admins.", .. ex.ExtractMessages()]);
    }
  }
}
