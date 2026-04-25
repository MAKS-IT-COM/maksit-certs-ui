import { object, string, ZodType } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../PatchRequestModelBase'

export interface PatchApiKeyRequest extends PatchRequestModelBase {
  description?: string
  expiresAt?: string
}

export const PatchApiKeyRequestSchema: ZodType<PatchApiKeyRequest> = PatchRequestModelBaseSchema.and(
  object({
    description: string().optional(),
    expiresAt: string().optional(),
  })
)
