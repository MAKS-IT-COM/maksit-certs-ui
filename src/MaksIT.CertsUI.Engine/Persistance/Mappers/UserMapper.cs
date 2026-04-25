using MaksIT.CertsUI.Engine.Domain.Identity;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Dto.Identity;

namespace MaksIT.CertsUI.Engine.Persistance.Mappers;

/// <summary>
/// Maps between <see cref="User"/> / <see cref="JwtToken"/> and <see cref="UserDto"/>. Used by identity persistence.
/// Pepper for recovery-code domain entities comes from <see cref="IIdentityDomainConfiguration"/> (same as Vault <c>UserMapper</c>).
/// </summary>
public class UserMapper(IIdentityDomainConfiguration identityConfiguration) {

  private readonly string _pepper = identityConfiguration.Pepper ?? string.Empty;

  public User MapToDomain(UserDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    DateTime? lastLogin = dto.LastLoginUtc == default ? null : dto.LastLoginUtc;

    return new User(dto.Id, dto.Name, dto.Salt, dto.Hash, lastLogin, dto.IsActive, dto.TwoFactorSharedKey,
        dto.TwoFactorRecoveryCodes.Select(rc => new TwoFactorRecoveryCode(rc.Id, rc.Salt, _pepper, rc.Hash, rc.IsUsed)))
      .SetTokens([.. dto.JwtTokens.Select(MapJwtTokenToDomain)]);
  }

  public static UserDto MapToDto(User user) {
    ArgumentNullException.ThrowIfNull(user);

    return new UserDto {
      Id = user.Id,
      Name = user.Username,
      Salt = user.PasswordSalt,
      Hash = user.PasswordHash,
      LastLoginUtc = user.LastLogin ?? default,
      IsActive = user.IsActive,
      TwoFactorSharedKey = user.TwoFactorSharedKey,
      JwtTokens = [.. user.Tokens.Select(jwt => new JwtTokenDto {
        Id = jwt.Id,
        UserId = user.Id,
        Token = jwt.Token,
        IssuedAt = jwt.IssuedAt,
        ExpiresAt = jwt.ExpiresAt,
        RefreshToken = jwt.RefreshToken,
        RefreshTokenExpiresAt = jwt.RefreshTokenExpiresAt,
        IsRevoked = jwt.IsRevoked,
      })],
      TwoFactorRecoveryCodes = [.. user.TwoFactorRecoveryCodes.Select(rc => new TwoFactorRecoveryCodeDto {
        Id = rc.Id,
        UserId = user.Id,
        Salt = rc.Salt,
        Hash = rc.Hash,
        IsUsed = rc.IsUsed,
      })],
    };
  }

  private static JwtToken MapJwtTokenToDomain(JwtTokenDto jt) =>
    new JwtToken(jt.Id, jt.Token, jt.IssuedAt, jt.ExpiresAt, jt.RefreshToken, jt.RefreshTokenExpiresAt)
      .SetIsRevoked(jt.IsRevoked);
}
