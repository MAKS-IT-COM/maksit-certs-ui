import z, { object, string, type ZodType } from 'zod'
import { PatchRequestModelBase } from '@maks-it.com/webui-contracts'
import { hasAnyFlag, ScopePermission } from '@maks-it.com/webui-core'
import { ScopeEntityType } from '../ScopeEntityType'

/** Mirrors <c>MaksIT.CertsUI.Models.Identity.User.PatchUserEntityScopeRequest</c>. */
export interface PatchUserEntityScopeRequest extends PatchRequestModelBase {
  id?: string | null
  entityId?: string | null
  entityType?: ScopeEntityType | null
  scope?: ScopePermission | null
}

export const PatchUserEntityScopeRequestSchema: ZodType<PatchUserEntityScopeRequest> = object({
  id: string().optional().nullable(),
  entityId: string().optional().nullable(),
  entityType: z.enum(ScopeEntityType).optional().nullable(),
  scope: z.number().optional().nullable(),
}).superRefine((val, ctx) => {
  if (val.entityType === undefined || val.entityType === null || val.scope === undefined || val.scope === null) {
    return
  }

  if (
    (val.entityType === ScopeEntityType.Identity || val.entityType === ScopeEntityType.ApiKey) &&
    !hasAnyFlag(
      val.scope,
      ScopePermission.Read |
        ScopePermission.Write |
        ScopePermission.Delete |
        ScopePermission.Create
    )
  ) {
    ctx.addIssue({
      code: 'custom',
      message: 'invalid scope permission value',
      path: ['entityScopes'],
    })
  }
})
