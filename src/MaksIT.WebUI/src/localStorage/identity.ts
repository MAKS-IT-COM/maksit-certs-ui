import { LoginResponse } from '../models/identity/login/LoginResponse'

/**
 * Maps API/localStorage payloads to a single camelCase LoginResponse so stored JSON matches across apps.
 */
const normalizeLoginResponse = (raw: unknown): LoginResponse | undefined => {
  if (raw == null || typeof raw !== 'object') return undefined
  const r = raw as Record<string, unknown>
  const str = (camel: keyof LoginResponse, pascal: string): string => {
    const v = r[camel as string] ?? r[pascal]
    if (v == null) return ''
    return typeof v === 'string' ? v : String(v)
  }
  const token = str('token', 'Token')
  const refreshToken = str('refreshToken', 'RefreshToken')
  if (!token || !refreshToken) return undefined

  const out: LoginResponse = {
    tokenType: str('tokenType', 'TokenType') || 'Bearer',
    token,
    expiresAt: str('expiresAt', 'ExpiresAt'),
    refreshToken,
    refreshTokenExpiresAt: str('refreshTokenExpiresAt', 'RefreshTokenExpiresAt'),
  }
  const u = r.username ?? r.Username
  if (typeof u === 'string' && u.length > 0)
    out.username = u

  return out
}

const readIdentity = (): LoginResponse | undefined => {
  const json = localStorage.getItem('identity')
  if (!json) return undefined
  try {
    return normalizeLoginResponse(JSON.parse(json) as unknown)
  } catch {
    return undefined
  }
}

const writeIdentity = (identity: LoginResponse | unknown) => {
  const n = normalizeLoginResponse(identity)
  if (n) localStorage.setItem('identity', JSON.stringify(n))
}

const removeIdentity = () => {
  localStorage.removeItem('identity')
}

export { readIdentity, writeIdentity, removeIdentity, normalizeLoginResponse }
