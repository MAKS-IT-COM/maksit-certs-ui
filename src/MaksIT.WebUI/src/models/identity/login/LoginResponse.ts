export interface LoginResponse {
    tokenType: string
    token: string
    expiresAt: string
    refreshToken: string
    refreshTokenExpiresAt: string
    /** Actual login username from the server; use for display so it is not replaced by a display name (e.g. "Organization Admin") from the JWT. */
    username?: string
}