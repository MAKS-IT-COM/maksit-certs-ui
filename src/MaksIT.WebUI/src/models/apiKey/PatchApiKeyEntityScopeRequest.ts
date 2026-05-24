import z, { object, string, type ZodType } from 'zod'
import { PatchRequestModelBase } from '@maks-it.com/webui-contracts'
import { hasAnyFlag, ScopePermission } from '@maks-it.com/webui-core'
import { ScopeEntityType } from '../ScopeEntityType'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.PatchApiKeyEntityScopeRequest</c>. */
export interface PatchApiKeyEntityScopeRequest extends PatchRequestModelBase {
  id?: string | null
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}

export const PatchApiKeyEntityScopeRequestSchema: ZodType<PatchApiKeyEntityScopeRequest> = object({
  id: string().optional().nullable(),
  entityId: string(),
  entityType: z.enum(ScopeEntityType),
  scope: z.number(),
}).superRefine((val, ctx) => {
  if (val.entityType === undefined || val.scope === undefined) {
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
