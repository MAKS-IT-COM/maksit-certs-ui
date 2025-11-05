
import z, { object, record, string } from 'zod'
import { RequestModelBase, RequestModelBaseSchema } from './RequestModelBase'
import { PatchOperation } from './PatchOperation'

export interface PatchRequestModelBase extends RequestModelBase {
    operations?: { [key: string]: PatchOperation }
}

export const PatchRequestModelBaseSchema = RequestModelBaseSchema.and(
  object({
    operations: record(string(), z.enum(PatchOperation)).optional()
  })
)