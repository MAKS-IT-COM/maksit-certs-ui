import { ResponseModelBase } from '../../../ResponseModelBase'

export interface SearchAPIKeyResponse extends ResponseModelBase {
  id: string
  createdAt: string
  description?: string
  expiresAt?: string
  revokedAt?: string
}
