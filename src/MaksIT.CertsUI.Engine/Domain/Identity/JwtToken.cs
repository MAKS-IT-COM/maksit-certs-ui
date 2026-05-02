using MaksIT.Results;
using MaksIT.Core.Abstractions.Domain;
using MaksIT.CertsUI.Engine.Facades;


namespace MaksIT.CertsUI.Engine.Domain.Identity;

/// <summary>
/// Constructs a JwtToken from a DTO with the specified id, token, issuedAt, expiresAt, refreshToken, and refreshTokenExpiresAt.
/// </summary>
/// <param name="id"></param>
/// <param name="token"></param>
/// <param name="issuedAt"></param>
/// <param name="expiresAt"></param>
/// <param name="refreshToken"></param>
/// <param name="refreshTokenExpiresAt"></param>
public class JwtToken(
  Guid id,
  string token,
  DateTime issuedAt,
  DateTime expiresAt,
  string refreshToken,
  DateTime refreshTokenExpiresAt
) : DomainDocumentBase<Guid>(id) {

  /// <summary>
  /// Represents a JSON Web Token (JWT) used for authentication and authorization.
  /// </summary>
  public string TokenType { get; private set; } = "Bearer";

  /// <summary>
  /// The actual JWT token string.
  /// </summary>
  public string Token { get; private set; } = token;

  /// <summary>
  /// The date and time when the JWT was issued.
  /// </summary>
  public DateTime IssuedAt { get; private set; } = issuedAt;

  /// <summary>
  /// The date and time when the JWT will expire.
  /// </summary>
  public DateTime ExpiresAt { get; private set; } = expiresAt;

  /// <summary>
  /// Indicates whether the JWT has been revoked.
  /// </summary>
  public bool IsRevoked { get; private set; } = false;

  /// <summary>
  /// The refresh token associated with this JWT, used to obtain a new JWT when the current one expires.
  /// </summary>
  public string RefreshToken { get; private set; } = refreshToken;

  /// <summary>
  /// The date and time when the refresh token will expire.
  /// </summary>
  public DateTime RefreshTokenExpiresAt { get; private set; } = refreshTokenExpiresAt;

  #region New entity constructor
  /// <summary>
  /// Constructs a new JwtToken with the specified token, issuedAt, expiresAt, refreshToken, and refreshTokenExpiresAt.
  /// </summary>
  /// <param name="token"></param>
  /// <param name="issuedAt"></param>
  /// <param name="expiresAt"></param>
  /// <param name="refreshToken"></param>
  /// <param name="refreshTokenExpiresAt"></param>
  public JwtToken(
    string token,
    DateTime issuedAt,
    DateTime expiresAt,
    string refreshToken,
    DateTime refreshTokenExpiresAt
  ) : this(CombGui.GenerateCombGuid(), token, issuedAt, expiresAt, refreshToken, refreshTokenExpiresAt) { }

  #endregion

  #region Fluent API for setting properties
  /// <summary>
  /// Sets the token type for this JwtToken instance.
  /// </summary>
  /// <param name="tokenType"></param>
  /// <returns></returns>
  public JwtToken SetTokenType(string tokenType) {
    TokenType = tokenType;
    return this;
  }

  /// <summary>
  /// Sets the actual JWT token string for this JwtToken instance.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  public JwtToken SetToken(string token) {
    Token = token;
    return this;
  }

  /// <summary>
  /// Sets the date and time when the JWT was issued.
  /// </summary>
  /// <param name="issuedAt"></param>
  /// <returns></returns>
  public JwtToken SetIssuedAt(DateTime issuedAt) {
    IssuedAt = issuedAt;
    return this;
  }

  /// <summary>
  /// Sets the date and time when the JWT will expire.
  /// </summary>
  /// <param name="expiresAt"></param>
  /// <returns></returns>
  public JwtToken SetExpiresAt(DateTime expiresAt) {
    ExpiresAt = expiresAt;
    return this;
  }

  /// <summary>
  /// Sets whether the JWT has been revoked.
  /// </summary>
  /// <param name="isRevoked"></param>
  /// <returns></returns>
  public JwtToken SetIsRevoked(bool isRevoked) {
    IsRevoked = isRevoked;
    return this;
  }

  /// <summary>
  /// Sets the refresh token for this JwtToken instance.
  /// </summary>
  /// <param name="refreshToken"></param>
  /// <returns></returns>
  public JwtToken SetRefreshToken(string refreshToken) {
    RefreshToken = refreshToken;
    return this;
  }

  /// <summary>
  /// Sets the date and time when the refresh token will expire.
  /// </summary>
  /// <param name="refreshTokenExpiresAt"></param>
  /// <returns></returns>
  public JwtToken SetRefreshTokenExpiresAt(DateTime refreshTokenExpiresAt) {
    RefreshTokenExpiresAt = refreshTokenExpiresAt;
    return this;
  }
  #endregion

  /// <summary>
  /// Revokes the JWT token, marking it as no longer valid.
  /// </summary>
  /// <returns></returns>
  public Result RevokeToken() {
    if (IsRevoked)
      return Result.Conflict("Token is already revoked.");
    SetIsRevoked(true);
    return Result.Ok();
  }

  /// <summary>
  /// Checks if the JWT token is valid based on its expiration and revocation status.
  /// </summary>
  /// <returns></returns>
  public bool IsValid() {
    return !IsRevoked && DateTime.UtcNow < ExpiresAt;
  }

  /// <summary>
  /// Checks if the refresh token is valid based on its expiration and revocation status.
  /// </summary>
  /// <returns></returns>
  public bool IsRefreshTokenValid() {
    return !IsRevoked && DateTime.UtcNow < RefreshTokenExpiresAt;
  }
}

