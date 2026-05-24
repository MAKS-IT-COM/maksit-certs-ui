import { object, string, type ZodType } from 'zod'
import { PagedRequest, PagedRequestSchema } from '@maks-it.com/webui-contracts'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchApiKeyEntityScopeRequest</c>. */
export interface SearchApiKeyEntityScopeRequest extends PagedRequest {
  apiKeyId?: string
}

export const SearchApiKeyEntityScopeRequestSchema: ZodType<SearchApiKeyEntityScopeRequest> = PagedRequestSchema.and(
  object({
    apiKeyId: string().optional(),
  })
)
