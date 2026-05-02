using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.CertsUI.Engine;
using MaksIT.CertsUI.Models.Identity.User;
using MaksIT.CertsUI.Models.Identity.User.Search;

namespace MaksIT.CertsUI.Mappers;

/// <summary>
/// Maps User / UserAuthorization and UserQueryResult to API response models.
/// Used by IdentityService.
/// </summary>
public class UserToResponseMapper {

  public UserResponse MapToResponse(User domain, UserAuthorization? authorization) {
    ArgumentNullException.ThrowIfNull(domain);
    return new UserResponse {
      Id = domain.Id,
      Username = domain.Username,
      Email = domain.Email,
      MobileNumber = domain.MobileNumber,
      IsActive = domain.IsActive,

      TwoFactorEnabled = domain.TwoFactorEnabled,
      RecoveryCodesLeft = domain.TwoFactorRecoveryCodes.Count(x => !x.IsUsed),

      IsGlobalAdmin = authorization?.IsGlobalAdmin ?? false,
      EntityScopes = (authorization?.EntityScopes ?? []).Select(sc => new UserEntityScopeResponse {
        Id = sc.Id,
        EntityId = sc.EntityId,
        EntityType = sc.EntityType,
        Scope = sc.Scope
      }).ToList(),
    };
  }

  public SearchUserResponse MapToSearchResponse(UserQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchUserResponse {
      Id = queryResult.Id,
      Username = queryResult.Username,
      Email = queryResult.Email,
      MobileNumber = queryResult.MobileNumber,
      IsActive = queryResult.IsActive,

      TwoFactorEnabled = queryResult.TwoFactorEnabled,
      RecoveryCodesLeft = queryResult.RecoveryCodesLeft,

      CreatedAt = queryResult.CreatedAt,
      LastLogin = queryResult.LastLogin,

      IsGlobalAdmin = queryResult.IsGlobalAdmin
    };
  }

  public SearchUserEntityScopeResponse MapToSearchResponse(UserEntityScopeQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchUserEntityScopeResponse {
      Id = queryResult.Id,
      UserId = queryResult.UserId,
      Username = queryResult.Username,
      EntityId = queryResult.EntityId,
      EntityName = queryResult.EntityName,
      EntityType = queryResult.EntityType,
      Scope = queryResult.Scope
    };
  }
}
