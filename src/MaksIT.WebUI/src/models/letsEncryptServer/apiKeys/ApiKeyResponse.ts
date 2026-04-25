import { ResponseModelBase } from '../../ResponseModelBase'

/** Plaintext <c>apiKey</c> only on create. */
export interface ApiKeyResponse extends ResponseModelBase {
  id: string
  apiKey: string
  createdAt: string
  description?: string
  expiresAt?: string
  revokedAt?: string
}
