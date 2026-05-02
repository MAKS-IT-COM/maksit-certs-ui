import { boolean, object, Schema, string } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../../PatchRequestModelBase'

export interface PatchHostnameRequest extends PatchRequestModelBase {
  hostname?: string
  isDisabled?: boolean
}

export const PatchHostnameRequestSchema: Schema<PatchHostnameRequest> = PatchRequestModelBaseSchema.and(
  object({
    hostname: string().optional(),
    isDisabled: boolean().optional()
  })
)
