using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistance.Mappers;

/// <summary>
/// Maps between <see cref="ApiKey"/> and <see cref="ApiKeyDto"/>. Used by API key persistence.
/// </summary>
public class ApiKeyMapper {

  public static ApiKey MapToDomain(ApiKeyDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    return new ApiKey(dto.Id, dto.KeySalt, dto.KeyHashHex, dto.CreatedAtUtc)
      .SetDescription(dto.Description)
      .SetExpiresAt(dto.ExpiresAtUtc)
      .SetRevokedAtUtc(dto.RevokedAtUtc);
  }

  public static ApiKeyDto MapToDto(ApiKey apiKey) {
    ArgumentNullException.ThrowIfNull(apiKey);

    return new ApiKeyDto {
      Id = apiKey.Id,
      KeySalt = apiKey.KeySalt,
      KeyHashHex = apiKey.KeyHashHex,
      Description = apiKey.Description,
      CreatedAtUtc = apiKey.CreatedAt,
      ExpiresAtUtc = apiKey.ExpiresAt,
      RevokedAtUtc = apiKey.RevokedAtUtc,
    };
  }
}
