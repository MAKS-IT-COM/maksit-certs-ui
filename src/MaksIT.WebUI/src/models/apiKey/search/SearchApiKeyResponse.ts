import type { ResponseModelBase } from '@maks-it.com/webui-contracts'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchAPIKeyResponse</c>. */
export interface SearchApiKeyResponse extends ResponseModelBase {
  id: string
  createdAt: string
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin: boolean
}
