import { ResponseModelBase } from '../../../ResponseModelBase'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchAPIKeyResponse</c>. */
export interface SearchAPIKeyResponse extends ResponseModelBase {
  id: string
  createdAt: string
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin: boolean
}
