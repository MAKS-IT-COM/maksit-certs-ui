import { object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'

export interface CreateApiKeyRequest extends RequestModelBase {
  description?: string
  expiresAt?: string
}

export const CreateApiKeyRequestSchema: Schema<CreateApiKeyRequest> = object({
  description: string().optional(),
  expiresAt: string().optional(),
})
