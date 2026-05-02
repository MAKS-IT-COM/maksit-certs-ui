import { ResponseModelBase } from '../../ResponseModelBase'
import type { ApiKeyEntityScopeResponse } from './ApiKeyEntityScopeResponse'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.ApiKeyResponse</c>. Plaintext <c>apiKey</c> when issued or loaded per server rules. */
export interface ApiKeyResponse extends ResponseModelBase {
  id: string
  apiKey: string
  createdAt: string
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin: boolean
  entityScopes?: ApiKeyEntityScopeResponse[] | null
}
