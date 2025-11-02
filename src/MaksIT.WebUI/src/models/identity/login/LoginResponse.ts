export interface LoginResponse {
    tokenType: string
    token: string
    expiresAt: string
    refreshToken: string
    refreshTokenExpiresAt: string
}