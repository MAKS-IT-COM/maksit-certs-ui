import { PatchHostnameRequest, PatchHostnameRequestSchema } from './PatchHostnameRequest'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '@maks-it.com/webui-contracts'
import { array, boolean, object, string, type ZodType } from 'zod'

export interface PatchAccountRequest extends PatchRequestModelBase {
  description?: string
  isDisabled?: boolean
  contacts?: string[]
  hostnames?: PatchHostnameRequest[]
}

export const PatchAccountRequestSchema: ZodType<PatchAccountRequest> = PatchRequestModelBaseSchema.and(
  object({
    description: string().optional(),
    isDisabled: boolean().optional(),
    contacts: array(string()).optional(),
    hostnames: array(PatchHostnameRequestSchema).optional()
  })
)

