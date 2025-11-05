import { PatchHostnameRequest, PatchHostnameRequestSchema } from './PatchHostnameRequest'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../../PatchRequestModelBase'
import { array, boolean, object, Schema, string } from 'zod'

export interface PatchAccountRequest extends PatchRequestModelBase {
  description?: string
  isDisabled?: boolean
  contacts?: string[]
  hostnames?: PatchHostnameRequest[]
}

export const PatchAccountRequestSchema: Schema<PatchAccountRequest> = PatchRequestModelBaseSchema.and(
  object({
    description: string().optional(),
    isDisabled: boolean().optional(),
    contacts: array(string()).optional(),
    hostnames: array(PatchHostnameRequestSchema).optional()
  })
)

