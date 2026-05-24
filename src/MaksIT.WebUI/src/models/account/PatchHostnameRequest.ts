import { boolean, object, string, type ZodType } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '@maks-it.com/webui-contracts'

export interface PatchHostnameRequest extends PatchRequestModelBase {
  hostname?: string
  isDisabled?: boolean
}

export const PatchHostnameRequestSchema: ZodType<PatchHostnameRequest> = PatchRequestModelBaseSchema.and(
  object({
    hostname: string().optional(),
    isDisabled: boolean().optional()
  })
)
