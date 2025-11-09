using MaksIT.Core.Abstractions.Domain;
using System.Linq.Dynamic.Core.Tokenizer;

namespace MaksIT.LetsEncryptServer.Domain;

public class JwtToken(Guid id) : DomainDocumentBase<Guid>(id) {


  /// <summary>
  /// Represents a JSON Web Token (JWT) used for authentication and authorization.
  /// </summary>
  public string TokenType { get; private set; } = "Bearer";

  /// <summary>
  /// The actual JWT token string.
  /// </summary>
  public string Token { get; private set; } = string.Empty;

  /// <summary>
  /// The date and time when the JWT was issued.
  /// </summary>
  public DateTime IssuedAt { get; private set; }

  /// <summary>
  /// The date and time when the JWT will expire.
  /// </summary>
  public DateTime ExpiresAt { get; private set; }

  /// <summary>
  /// Indicates whether the JWT has been revoked.
  /// </summary>
  public bool IsRevoked { get; private set; } = false;

  /// <summary>
  /// The refresh token associated with this JWT, used to obtain a new JWT when the current one expires.
  /// </summary>
  public string RefreshToken { get; private set; } = string.Empty;

  /// <summary>
  /// The date and time when the refresh token will expire.
  /// </summary>
  public DateTime RefreshTokenExpiresAt { get; private set; }

  public JwtToken() : this(Guid.NewGuid()) { }

  public JwtToken SetAccessTokenData(
    string token,
    DateTime issuedAt,
    DateTime expiresAt
  ) {
    Token = token;
    IssuedAt = issuedAt;
    ExpiresAt = expiresAt;
    return this;
  }

  public JwtToken SetRefreshTokenData(
    string refreshToken,
    DateTime refreshTokenExpiresAt
  ) {
    RefreshToken = refreshToken;
    RefreshTokenExpiresAt = refreshTokenExpiresAt;
    return this;
  }
}
