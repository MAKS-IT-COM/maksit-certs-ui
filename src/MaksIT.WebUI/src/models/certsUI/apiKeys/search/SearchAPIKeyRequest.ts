import { PagedRequest } from '../../../PagedRequest'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchAPIKeyRequest</c>. */
export interface SearchAPIKeyRequest extends PagedRequest {
  organizationFilters?: string | null
  applicationFilters?: string | null
}
