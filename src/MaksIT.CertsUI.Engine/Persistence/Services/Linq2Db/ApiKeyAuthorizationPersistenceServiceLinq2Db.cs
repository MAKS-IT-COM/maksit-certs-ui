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
/// Linq2Db-based implementation of <see cref="IApiKeyAuthorizationPersistenceService"/>.
/// </summary>
public class ApiKeyAuthorizationPersistenceServiceLinq2Db(ILogger<ApiKeyAuthorizationPersistenceServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IApiKeyAuthorizationPersistenceService {
  private readonly ILogger<ApiKeyAuthorizationPersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<ApiKeyAuthorization?> ReadByApiKeyId(Guid apiKeyId) {
    try {
      using var db = _connectionFactory.Create();

      var dto = db.GetTable<ApiKeyDto>()
        .Where(k => k.Id == apiKeyId)
        .FirstOrDefault();

      if (dto == null)
        return Result<ApiKeyAuthorization?>.NotFound(null, $"API key {apiKeyId} not found.");

      dto.EntityScopes = [.. db.GetTable<ApiKeyEntityScopeDto>()
          .Where(s => s.ApiKeyId == apiKeyId)];

      return Result<ApiKeyAuthorization?>.Ok(ApiKeyMapper.ToAuthorization(dto));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading authorization for API key {ApiKeyId}", apiKeyId);
      return Result<ApiKeyAuthorization?>.InternalServerError(null, ["An error occurred while retrieving API key authorization.", .. ex.ExtractMessages()]);
    }
  }

  public Result Write(ApiKeyAuthorization authorization)
  {
    if (authorization == null)
      return Result.BadRequest("Authorization is null.");

    try
    {
      using var db = _connectionFactory.Create();

      if (!db.GetTable<ApiKeyDto>().Any(k => k.Id == authorization.ApiKeyId))
        return Result.NotFound($"API key {authorization.ApiKeyId} not found.");

      db.GetTable<ApiKeyDto>().Where(k => k.Id == authorization.ApiKeyId)
        .Set(k => k.IsGlobalAdmin, authorization.IsGlobalAdmin).Update();

      var existing = db.GetTable<ApiKeyEntityScopeDto>()
        .Where(s => s.ApiKeyId == authorization.ApiKeyId)
        .ToList();

      var desired = ApiKeyMapper
        .ToEntityScopeDtos(authorization.EntityScopes, authorization.ApiKeyId);

      var desiredKeys = desired
        .Select(s => (s.EntityId, s.EntityType, s.Scope))
        .ToHashSet();

      foreach (var e in existing.Where(s => !desiredKeys.Contains((s.EntityId, s.EntityType, s.Scope))))
        db.GetTable<ApiKeyEntityScopeDto>()
          .Where(s => s.Id == e.Id).Delete();

      foreach (var d in desired) {
        var match = existing.FirstOrDefault(x => x.EntityId == d.EntityId && x.EntityType == d.EntityType && x.Scope == d.Scope);

        if (match == null) {
          d.ApiKeyId = authorization.ApiKeyId; db.Insert(d);
        }
        else if (match.Id != d.Id) {
          db.GetTable<ApiKeyEntityScopeDto>()
            .Where(s => s.Id == match.Id)
            .Delete();

          d.ApiKeyId = authorization.ApiKeyId; db.Insert(d);
        }
        else {
          db.GetTable<ApiKeyEntityScopeDto>().Where(s => s.Id == d.Id)
            .Set(s => s.EntityId, d.EntityId)
            .Set(s => s.EntityType, d.EntityType)
            .Set(s => s.Scope, d.Scope).Update();
        }
      }

      return Result.Ok();
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error writing authorization for API key {ApiKeyId}", authorization.ApiKeyId);
      return Result.InternalServerError(["An error occurred while saving API key authorization.", .. ex.ExtractMessages()]);
    }
  }
}
