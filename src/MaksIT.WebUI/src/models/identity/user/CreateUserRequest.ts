import { object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'

export interface CreateUserRequest extends RequestModelBase {
  username: string
  password: string
}

export const CreateUserRequestSchema: Schema<CreateUserRequest> = object({
  username: string().min(1),
  password: string().min(8),
})
