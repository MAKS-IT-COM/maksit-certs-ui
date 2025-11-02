import { array, object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'
import { CreateUserEntityScopeRequest, CreateUserEntityScopeRequestSchema } from './CreateUserEntityScopeRequest'

export interface CreateUserRequest extends RequestModelBase {
    username: string
    email: string
    mobileNumber: string
    password: string
    entityScopes?: CreateUserEntityScopeRequest[]
}

export const CreateUserRequestSchema: Schema<CreateUserRequest> = object({
  username: string().min(1),
  email: string(),
  mobileNumber: string().min(1),
  password: string().min(8),
  entityScopes: array(CreateUserEntityScopeRequestSchema).optional()
})