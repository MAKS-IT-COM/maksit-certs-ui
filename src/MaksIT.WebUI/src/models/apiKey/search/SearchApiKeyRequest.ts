import { object, string, type ZodType } from 'zod'
import { PagedRequest, PagedRequestSchema } from '@maks-it.com/webui-contracts'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchAPIKeyRequest</c>. */
export interface SearchApiKeyRequest extends PagedRequest {
  organizationFilters?: string | null
  applicationFilters?: string | null
}

export const SearchApiKeyRequestSchema: ZodType<SearchApiKeyRequest> = PagedRequestSchema.and(
  object({
    organizationFilters: string().optional().nullable(),
    applicationFilters: string().optional().nullable(),
  })
)
