import z, { object, string, type ZodType } from 'zod'
import { RequestModelBase } from '@maks-it.com/webui-contracts'
import { hasAnyFlag, ScopePermission } from '@maks-it.com/webui-core'
import { ScopeEntityType } from '../ScopeEntityType'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.CreateApiKeyEntityScopeRequest</c>. */
export interface CreateApiKeyEntityScopeRequest extends RequestModelBase {
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}

export const CreateApiKeyEntityScopeRequestSchema: ZodType<CreateApiKeyEntityScopeRequest> = object({
  entityId: string(),
  entityType: z.enum(ScopeEntityType),
  scope: z.number(),
}).superRefine((val, ctx) => {
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
