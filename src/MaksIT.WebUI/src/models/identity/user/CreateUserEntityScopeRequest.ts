import z, { object, Schema, string } from 'zod'
import { RequestModelBase } from '../../RequestModelBase'
import { ScopeEntityType } from '../ScopeEntityType'
import { ScopePermission } from '../ScopePermissions'
import { hasAnyFlag } from '../../../functions'

export interface CreateUserEntityScopeRequest extends RequestModelBase {
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}

export const CreateUserEntityScopeRequestSchema: Schema<CreateUserEntityScopeRequest> = object({
  entityId: string(),
  entityType: z.enum(ScopeEntityType),
  scope: z.number()
}).superRefine((val: CreateUserEntityScopeRequest, ctx) => {
  
  if (val.entityType == ScopeEntityType.Organization && !hasAnyFlag(val.scope,
    ScopePermission.Read | ScopePermission.Write
  )) {
    ctx.addIssue({
      code: 'custom',
      message: 'invalid scope permission value',
      path: ['entityScopes'],
    })
  }

  if (val.entityType == ScopeEntityType.Application && !hasAnyFlag(val.scope,
    ScopePermission.Read | ScopePermission.Write | ScopePermission.Delete | ScopePermission.Create
  )) {
    ctx.addIssue({
      code: 'custom',
      message: 'invalid scope permission value',
      path: ['entityScopes'],
    })
  }
})