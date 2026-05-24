import { object, string, type ZodType } from 'zod'
import { PagedRequest, PagedRequestSchema } from '@maks-it.com/webui-contracts'

export interface SearchUserEntityScopeRequest extends PagedRequest {
  userId?: string
}

export const SearchUserEntityScopeRequestSchema: ZodType<SearchUserEntityScopeRequest> = PagedRequestSchema.and(
  object({
    userId: string().optional(),
  })
)
