using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Models.CertsUI.Account.Responses;

namespace MaksIT.CertsUI.Mappers;

/// <summary>
/// Maps RegistrationCache and CachedHostname to API response models.
/// Used by AccountService.
/// </summary>
public class AccountToResponseMapper {

  public GetHostnameResponse MapToResponse(CachedHostname host) {
    ArgumentNullException.ThrowIfNull(host);
    return new GetHostnameResponse {
      Hostname = host.Hostname,
      Expires = host.Expires,
      IsUpcomingExpire = host.IsUpcomingExpire,
      IsDisabled = host.IsDisabled
    };
  }

  public GetAccountResponse MapToResponse(Guid accountId, RegistrationCache cache) {
    ArgumentNullException.ThrowIfNull(cache);
    var hostnames = cache.GetHosts().Select(MapToResponse).ToArray();

    return new GetAccountResponse {
      AccountId = accountId,
      IsDisabled = cache.IsDisabled,
      Description = cache.Description,
      Contacts = cache.Contacts,
      ChallengeType = cache.ChallengeType,
      Hostnames = hostnames,
      IsStaging = cache.IsStaging
    };
  }
}
