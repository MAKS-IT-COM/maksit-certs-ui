using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistence.Mappers;

/// <summary>
/// Maps between User / UserAuthorization and UserDto. Used by identity and user-authorization persistence.
/// </summary>
public class UserMapper(string passwordPepper) {
  private readonly string _passwordPepper = passwordPepper ?? string.Empty;

  public User MapToDomain(UserDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    return new User(
      dto.Id,
      dto.Username,
      dto.PasswordSalt,
      dto.PasswordHash,
      dto.CreatedAt
    )
    .SetEmail(dto.Email)
    .SetMobileNumber(dto.MobileNumber)
    .SetIsActive(dto.IsActive)
    .SetTwoFactorSharedKey(dto.TwoFactorSharedKey)
    .SetTwoFactorRecoveryCodes([.. dto.TwoFactorRecoveryCodes.Select(rc => new TwoFactorRecoveryCode(rc.Id, rc.Salt, _passwordPepper, rc.Hash, rc.IsUsed))])
    .SetTokens([.. dto.JwtTokens.Select(jwt => new JwtToken(jwt.Id, jwt.Token, jwt.IssuedAt, jwt.ExpiresAt, jwt.RefreshToken, jwt.RefreshTokenExpiresAt).SetIsRevoked(jwt.IsRevoked))]);
  }

  public static UserDto MapToDto(User user) {
    ArgumentNullException.ThrowIfNull(user);

    return new UserDto {
      Id = user.Id,
      Username = user.Username,
      Email = user.Email,
      MobileNumber = user.MobileNumber,
      IsActive = user.IsActive,
      PasswordSalt = user.PasswordSalt,
      PasswordHash = user.PasswordHash,
      TwoFactorSharedKey = user.TwoFactorSharedKey,
      CreatedAt = user.CreatedAt,
      LastLogin = user.LastLogin,
      TwoFactorRecoveryCodes = [.. user.TwoFactorRecoveryCodes.Select(rc => new TwoFactorRecoveryCodeDto {
        Id = rc.Id,
        Salt = rc.Salt,
        Hash = rc.Hash,
        IsUsed = rc.IsUsed,
        UserId = user.Id
      })],
      JwtTokens = [.. user.Tokens.Select(jwt => new JwtTokenDto {
        Id = jwt.Id,
        Token = jwt.Token,
        IssuedAt = jwt.IssuedAt,
        ExpiresAt = jwt.ExpiresAt,
        RefreshToken = jwt.RefreshToken,
        RefreshTokenExpiresAt = jwt.RefreshTokenExpiresAt,
        IsRevoked = jwt.IsRevoked,
        UserId = user.Id
      })]
    };
  }

  /// <summary>Applies authorization to a DTO (IsGlobalAdmin and EntityScopes). Syncs EntityScopes in place so the persistence layer can diff existing vs desired without duplicate-key issues.</summary>
  public static void ApplyAuthorizationToDto(UserDto dto, Guid userId, UserAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(dto);

    dto.IsGlobalAdmin = authorization?.IsGlobalAdmin ?? false;

    dto.EntityScopes ??= [];

    SyncEntityScopesToDto(dto.EntityScopes, authorization?.EntityScopes ?? [], userId);
  }

  /// <summary>Syncs a DTO's EntityScopes list to match the domain list. Domain guarantees unique Ids in newScopes.</summary>
  private static void SyncEntityScopesToDto(List<UserEntityScopeDto> current, List<UserEntityScope> newScopes, Guid userId) {
    var desired = ToEntityScopeDtos(newScopes, userId);
    var desiredKeys = desired.Select(s => (s.EntityId, s.EntityType, s.Scope)).ToHashSet();

    for (var i = current.Count - 1; i >= 0; i--) {
      var c = current[i];
      if (!desiredKeys.Contains((c.EntityId, c.EntityType, c.Scope)))
        current.RemoveAt(i);
    }

    foreach (var sc in desired) {
      var match = current.FirstOrDefault(x =>
        x.EntityId == sc.EntityId && x.EntityType == sc.EntityType && x.Scope == sc.Scope);

      if (match != null) {
        match.Id = sc.Id;
        match.EntityId = sc.EntityId;
        match.EntityType = sc.EntityType;
        match.Scope = sc.Scope;
      }
      else {
        current.Add(new UserEntityScopeDto {
          Id = sc.Id,
          UserId = userId,
          EntityId = sc.EntityId,
          EntityType = sc.EntityType,
          Scope = sc.Scope
        });
      }
    }
  }

  /// <summary>Builds UserAuthorization from a UserDto (read path for authorization persistence).</summary>
  public static UserAuthorization ToAuthorization(UserDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    return new UserAuthorization(
      dto.Id,
      dto.IsGlobalAdmin,
      [.. (dto.EntityScopes ?? []).Select(scope => new UserEntityScope(scope.Id)
        .SetEntityId(scope.EntityId)
        .SetEntityType(scope.EntityType)
        .SetScope(scope.Scope)
      )]);
  }

  public static List<UserEntityScopeDto> ToEntityScopeDtos(IEnumerable<UserEntityScope> scopes, Guid userId) {
    return [.. scopes.Select(scope => new UserEntityScopeDto {
      Id = scope.Id,
      UserId = userId,
      EntityId = scope.EntityId,
      EntityType = scope.EntityType,
      Scope = scope.Scope
    })];
  }
}
