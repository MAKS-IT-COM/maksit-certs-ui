namespace MaksIT.CertsUI.Authorization;

public class JwtTokenData {
  public required string Token { get; set; }

  public required string Username { get; set; }

  /// <summary>
  /// Frontend roles from the JWT token claims.
  /// </summary>
  public required List<string> ClaimRoles { get; set; }

  public required DateTime IssuedAt { get; set; }

  public required DateTime ExpiresAt { get; set; }

  public Guid UserId { get; set; }
  public bool IsGlobalAdmin { get; set; }

  public List<IdentityScopeData>? EntityScopes { get; set; }
}
