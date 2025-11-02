import z, { object, Schema, string } from 'zod'
import { PatchRequestModelBase } from '../../PatchRequestModelBase'
import { ScopeEntityType } from '../ScopeEntityType'
import { ScopePermission } from '../ScopePermissions'
import { hasAnyFlag } from '../../../functions'

export interface PatchUserEntityScopeRequest extends PatchRequestModelBase {
    id: string,
    entityId: string,
    entityType: ScopeEntityType,
    scope: ScopePermission
}

export const PatchUserEntityScopeRequestSchema: Schema<PatchUserEntityScopeRequest> =  object({
  id: string(),
  entityId: string(),
  entityType: z.enum(ScopeEntityType),
  scope: z.number()
}).superRefine((val: PatchUserEntityScopeRequest, ctx) => {
  
  if (val.entityType === ScopeEntityType.Organization && !hasAnyFlag(val.scope, ScopePermission.Read | ScopePermission.Write)) {
    ctx.addIssue({
      code: 'custom',
      message: 'Invalid scope permission value',
      path: ['scope']
    })
  }

  if (val.entityType === ScopeEntityType.Application && !hasAnyFlag(val.scope, ScopePermission.Read | ScopePermission.Write | ScopePermission.Delete | ScopePermission.Create)) {
    ctx.addIssue({
      code: 'custom',
      message: 'Invalid scope permission value',
      path: ['scope'],
    })
  }
})