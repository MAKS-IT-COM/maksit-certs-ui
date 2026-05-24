import { array, boolean, object, string, type ZodType } from 'zod'
import { RequestModelBase } from '@maks-it.com/webui-contracts'
import { withCreateGlobalAdminRefine } from '../engine/globalAdminZod'
import { CreateUserEntityScopeRequest, CreateUserEntityScopeRequestSchema } from './CreateUserEntityScopeRequest'

export interface CreateUserRequest extends RequestModelBase {
  username: string
  email: string
  mobileNumber: string
  password: string
  isGlobalAdmin?: boolean
  entityScopes?: CreateUserEntityScopeRequest[] | null
}

export const CreateUserRequestSchema: ZodType<CreateUserRequest> = object({
  username: string().min(1),
  email: string().email(),
  mobileNumber: string().min(1),
  password: string().min(8),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(CreateUserEntityScopeRequestSchema).optional().nullable(),
})

export const createCreateUserRequestSchema = (actorIsGlobalAdmin: boolean) =>
  withCreateGlobalAdminRefine(CreateUserRequestSchema, actorIsGlobalAdmin)
