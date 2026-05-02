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
/// Linq2Db-based implementation of <see cref="IApiKeyPersistenceService"/>.
/// </summary>
public class ApiKeyPersistenceServiceLinq2Db(ILogger<ApiKeyPersistenceServiceLinq2Db> logger, ICertsUIDataConnectionFactory connectionFactory) : IApiKeyPersistenceService {
  private readonly ILogger<ApiKeyPersistenceServiceLinq2Db> _logger = logger;
  private readonly ICertsUIDataConnectionFactory _connectionFactory = connectionFactory;

  public Result<ApiKey?> ReadById(Guid apiKeyId) =>
    GetSingle(db => db.GetTable<ApiKeyDto>()
      .Where(k => k.Id == apiKeyId), $"ID {apiKeyId}", $"API key with ID {apiKeyId} not found.");

  public Result<ApiKey?> ReadAPIKey(string apiKeyValue) =>
    GetSingle(db => db.GetTable<ApiKeyDto>()
      .Where(k => k.ApiKey == apiKeyValue), $"value {apiKeyValue}", $"API key with value {apiKeyValue} not found.");

  private Result<ApiKey?> GetSingle(Func<LinqToDB.Data.DataConnection, IQueryable<ApiKeyDto>> queryFn, string identifier, string notFoundMessage) {
    try {
      using var db = _connectionFactory.Create();

      var dto = queryFn(db).FirstOrDefault();

      if (dto == null)
        return Result<ApiKey?>.NotFound(null, notFoundMessage);

      dto.EntityScopes = [.. db.GetTable<ApiKeyEntityScopeDto>().Where(s => s.ApiKeyId == dto.Id)];
      return Result<ApiKey?>.Ok(ApiKeyMapper.MapToDomain(dto));
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error reading API key with {Identifier}", identifier);
      return Result<ApiKey?>.InternalServerError(null, ["An error occurred while retrieving the API key.", .. ex.ExtractMessages()]);
    }
  }

  public Result<ApiKey?> Write(ApiKey apiKey) => Write(apiKey, (ApiKeyAuthorization?)null);

  public Result<ApiKey?> Write(ApiKey apiKey, ApiKeyAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(apiKey);

    try {
      var dto = ApiKeyMapper.MapToDto(apiKey);

      ApiKeyMapper.ApplyAuthorizationToDto(dto, apiKey.Id, authorization);

      using var db = _connectionFactory.Create();

      var existing = db.GetTable<ApiKeyDto>().Where(k => k.Id == apiKey.Id).FirstOrDefault();

      if (existing != null) {
        existing.EntityScopes = [.. db.GetTable<ApiKeyEntityScopeDto>()
          .Where(s => s.ApiKeyId == apiKey.Id)];

        db.GetTable<ApiKeyDto>()
          .Where(k => k.Id == dto.Id)
          .Set(k => k.ApiKey, dto.ApiKey)
          .Set(k => k.Description, dto.Description)
          .Set(k => k.IsGlobalAdmin, dto.IsGlobalAdmin)
          .Set(k => k.CreatedAt, dto.CreatedAt)
          .Set(k => k.ExpiresAt, dto.ExpiresAt)
          .Update();

        SyncApiKeyEntityScopes(db, apiKey.Id, existing.EntityScopes, dto.EntityScopes);
      }
      else {
        db.Insert(dto);
        foreach (var s in dto.EntityScopes) { s.ApiKeyId = apiKey.Id; db.Insert(s); }
      }

      return Result<ApiKey?>.Ok(apiKey);
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error writing API key {ApiKeyId}", apiKey.Id);
      return Result<ApiKey?>.InternalServerError(null, ["An error occurred while saving the API key.", .. ex.ExtractMessages()]);
    }
  }

  private static void SyncApiKeyEntityScopes(LinqToDB.Data.DataConnection db, Guid apiKeyId, List<ApiKeyEntityScopeDto> existing, List<ApiKeyEntityScopeDto> desired) {
    var desiredKeys = desired.Select(s => (s.EntityId, s.EntityType, s.Scope)).ToHashSet();

    foreach (var e in existing.Where(s => !desiredKeys.Contains((s.EntityId, s.EntityType, s.Scope))))
      db.GetTable<ApiKeyEntityScopeDto>().Where(s => s.Id == e.Id).Delete();

    foreach (var d in desired) {
      var match = existing.FirstOrDefault(x => x.EntityId == d.EntityId && x.EntityType == d.EntityType && x.Scope == d.Scope);

      if (match == null) {
        d.ApiKeyId = apiKeyId; db.Insert(d);
      }
      else if (match.Id != d.Id) {
        db.GetTable<ApiKeyEntityScopeDto>()
        .Where(s => s.Id == match.Id).Delete();
        d.ApiKeyId = apiKeyId; db.Insert(d);
      }
      else {
        db.GetTable<ApiKeyEntityScopeDto>()
          .Where(s => s.Id == d.Id)
          .Set(s => s.EntityId, d.EntityId)
          .Set(s => s.EntityType, d.EntityType)
          .Set(s => s.Scope, d.Scope)
          .Update();
      }
    }
  }

  public Result<List<ApiKey>?> WriteMany(List<ApiKey> apiKeys) {
    ArgumentNullException.ThrowIfNull(apiKeys);

    if (apiKeys.Count == 0)
      return Result<List<ApiKey>?>.Ok(apiKeys);

    foreach (var k in apiKeys) {
      var r = Write(k, (ApiKeyAuthorization?)null);

      if (!r.IsSuccess)
        return r.ToResultOfType<List<ApiKey>?>(_ => (List<ApiKey>?)null);
    }

    return Result<List<ApiKey>?>.Ok(apiKeys);
  }

  public Result DeleteById(Guid apiKeyId) => DeleteMany([apiKeyId]);

  public Result DeleteMany(List<Guid> apiKeyIds) {
    ArgumentNullException.ThrowIfNull(apiKeyIds);

    try {
      using var db = _connectionFactory.Create();
      foreach (var id in apiKeyIds) {
        if (!db.GetTable<ApiKeyDto>().Any(k => k.Id == id))
          return Result.NotFound($"API key with ID {id} not found.");

        db.GetTable<ApiKeyEntityScopeDto>()
          .Where(s => s.ApiKeyId == id)
          .Delete();

        db.GetTable<ApiKeyDto>()
          .Where(k => k.Id == id)
          .Delete();
      }

      return Result.Ok();
    }
    catch (Exception ex) {
      if (_logger.IsEnabled(LogLevel.Error))
        _logger.LogError(ex, "Error deleting API keys");
      return Result.InternalServerError(["Error occurred while deleting API keys.", .. ex.ExtractMessages()]);
    }
  }
}
