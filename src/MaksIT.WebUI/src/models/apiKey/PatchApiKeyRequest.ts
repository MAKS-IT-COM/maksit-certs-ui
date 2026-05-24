import { array, boolean, object, string, ZodType } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '@maks-it.com/webui-contracts'
import { withPatchGlobalAdminRefine } from '../engine/globalAdminZod'
import { PatchApiKeyEntityScopeRequest, PatchApiKeyEntityScopeRequestSchema } from './PatchApiKeyEntityScopeRequest'

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
    entityScopes: array(PatchApiKeyEntityScopeRequestSchema).optional().nullable(),
  })
)

export const createPatchApiKeyRequestSchema = (actorIsGlobalAdmin: boolean) =>
  withPatchGlobalAdminRefine(PatchApiKeyRequestSchema, actorIsGlobalAdmin)
