using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Query.Identity;
using MaksIT.Models.LetsEncryptServer.Identity.User;
using MaksIT.Models.LetsEncryptServer.Identity.User.Search;

namespace MaksIT.CertsUI.Mappers;

/// <summary>
/// Maps User and UserQueryResult / UserEntityScopeQueryResult to API response models.
/// Used by IdentityService.
/// </summary>
public class UserToResponseMapper {

  public UserResponse MapToResponse(User domain) {
    ArgumentNullException.ThrowIfNull(domain);
    return new UserResponse {
      Id = domain.Id,
      Username = domain.Username,
      IsActive = domain.IsActive,

      TwoFactorEnabled = domain.TwoFactorEnabled,
      RecoveryCodesLeft = domain.TwoFactorRecoveryCodes.Count(x => !x.IsUsed),

      LastLogin = domain.LastLogin
    };
  }

  public SearchUserResponse MapToSearchResponse(UserQueryResult queryResult) {
    ArgumentNullException.ThrowIfNull(queryResult);
    return new SearchUserResponse {
      Id = queryResult.Id,
      Username = queryResult.Username,
      IsActive = queryResult.IsActive,

      TwoFactorEnabled = queryResult.TwoFactorEnabled,

      LastLogin = queryResult.LastLogin
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
