import { object, Schema } from 'zod'

export interface RequestModelBase {
    [key: string]: unknown; // Add index signature
}

export const RequestModelBaseSchema: Schema<RequestModelBase> = object({
  // Define the schema for the base request model
})