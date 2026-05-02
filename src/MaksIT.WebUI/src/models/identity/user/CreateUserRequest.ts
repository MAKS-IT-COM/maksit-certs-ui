import { array, boolean, number, object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'
import { guidStringSchema } from '../../guidString'
import { CreateUserEntityScopeRequest } from './CreateUserEntityScopeRequest'

export interface CreateUserRequest extends RequestModelBase {
  username: string
  email: string
  mobileNumber: string
  password: string
  isGlobalAdmin?: boolean
  entityScopes?: CreateUserEntityScopeRequest[] | null
}

export const CreateUserRequestSchema: Schema<CreateUserRequest> = object({
  username: string().min(1),
  email: string().email(),
  mobileNumber: string().min(1),
  password: string().min(8),
  isGlobalAdmin: boolean().optional(),
  entityScopes: array(object({
    entityId: guidStringSchema,
    entityType: number(),
    scope: number(),
  })).optional().nullable(),
})
