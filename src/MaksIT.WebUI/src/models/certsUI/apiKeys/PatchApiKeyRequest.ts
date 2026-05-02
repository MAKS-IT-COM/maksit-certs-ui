import { array, boolean, number, object, string, ZodType } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../PatchRequestModelBase'
import { guidStringSchema, guidStringSchemaOptionalNullable } from '../../guidString'
import { PatchApiKeyEntityScopeRequest } from './PatchApiKeyEntityScopeRequest'

export interface PatchApiKeyRequest extends PatchRequestModelBase {
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin?: boolean | null
  entityScopes?: PatchApiKeyEntityScopeRequest[] | null
}

export const PatchApiKeyRequestSchema: ZodType<PatchApiKeyRequest> = PatchRequestModelBaseSchema.and(
  object({
    description: string().optional().nullable(),
    expiresAt: string().optional().nullable(),
    isGlobalAdmin: boolean().optional().nullable(),
    entityScopes: array(object({
      id: guidStringSchemaOptionalNullable,
      entityId: guidStringSchema,
      entityType: number(),
      scope: number(),
    }).passthrough()).optional().nullable(),
  })
)
