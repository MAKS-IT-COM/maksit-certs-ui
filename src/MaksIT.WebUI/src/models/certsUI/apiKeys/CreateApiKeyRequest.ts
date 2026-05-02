import { array, boolean, number, object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'
import { guidStringSchema } from '../../guidString'
import { CreateApiKeyEntityScopeRequest } from './CreateApiKeyEntityScopeRequest'

export interface CreateApiKeyRequest extends RequestModelBase {
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin?: boolean
  entityScopes?: CreateApiKeyEntityScopeRequest[] | null
}

export const CreateApiKeyRequestSchema: Schema<CreateApiKeyRequest> = object({
  description: string().optional().nullable(),
  expiresAt: string().optional().nullable(),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(object({
    entityId: guidStringSchema,
    entityType: number(),
    scope: number(),
  })).optional().nullable(),
})
