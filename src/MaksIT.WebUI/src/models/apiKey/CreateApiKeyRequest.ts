import { array, boolean, object, string, type ZodType } from 'zod'
import { RequestModelBase } from '@maks-it.com/webui-contracts'
import { withCreateGlobalAdminRefine } from '../engine/globalAdminZod'
import { CreateApiKeyEntityScopeRequest, CreateApiKeyEntityScopeRequestSchema } from './CreateApiKeyEntityScopeRequest'

export interface CreateApiKeyRequest extends RequestModelBase {
  description?: string | null
  expiresAt?: string | null
  isGlobalAdmin?: boolean
  entityScopes?: CreateApiKeyEntityScopeRequest[] | null
}

export const CreateApiKeyRequestSchema: ZodType<CreateApiKeyRequest> = object({
  description: string().optional().nullable(),
  expiresAt: string().optional().nullable(),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(CreateApiKeyEntityScopeRequestSchema).optional().nullable(),
})

export const createCreateApiKeyRequestSchema = (actorIsGlobalAdmin: boolean) =>
  withCreateGlobalAdminRefine(CreateApiKeyRequestSchema, actorIsGlobalAdmin)
