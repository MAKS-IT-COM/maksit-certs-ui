import { object, string, type ZodType } from 'zod'
import { PagedRequest, PagedRequestSchema } from '@maks-it.com/webui-contracts'

export interface SearchUserRequest extends PagedRequest {
  organizationFilters?: string
  applicationFilters?: string
}

export const SearchUserRequestSchema: ZodType<SearchUserRequest> = PagedRequestSchema.and(
  object({
    organizationFilters: string().optional(),
    applicationFilters: string().optional(),
  })
)
