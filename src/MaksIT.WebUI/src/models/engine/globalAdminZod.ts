import z, { RefinementCtx } from 'zod'
import { PatchRequestModelBase } from '@maks-it.com/webui-contracts'

export const GLOBAL_ADMIN_ASSIGN_MESSAGE =
  'Only a global admin can create or assign global admin privileges.'

export const GLOBAL_ADMIN_PATCH_MESSAGE =
  'Only a global admin can assign or remove the global admin flag.'

type GlobalAdminAssignable = { isGlobalAdmin?: boolean | null }

type GlobalAdminPatchable = PatchRequestModelBase & GlobalAdminAssignable

export function refineCreateGlobalAdminAssignment(
  val: GlobalAdminAssignable,
  ctx: RefinementCtx,
  actorIsGlobalAdmin: boolean
) {
  if (val.isGlobalAdmin && !actorIsGlobalAdmin) {
    ctx.addIssue({
      code: 'custom',
      message: GLOBAL_ADMIN_ASSIGN_MESSAGE,
      path: ['isGlobalAdmin'],
    })
  }
}

export function refinePatchGlobalAdminFlag(
  val: GlobalAdminPatchable,
  ctx: RefinementCtx,
  actorIsGlobalAdmin: boolean
) {
  if (val.operations?.isGlobalAdmin != null && !actorIsGlobalAdmin) {
    ctx.addIssue({
      code: 'custom',
      message: GLOBAL_ADMIN_PATCH_MESSAGE,
      path: ['isGlobalAdmin'],
    })
  }
}

/** Wraps a create schema with global-admin assignment validation for the acting user/API key. */
export function withCreateGlobalAdminRefine<T extends z.ZodType<GlobalAdminAssignable>>(
  schema: T,
  actorIsGlobalAdmin: boolean
) {
  return schema.superRefine((val, ctx) =>
    refineCreateGlobalAdminAssignment(val, ctx, actorIsGlobalAdmin)
  )
}

/** Wraps a patch schema with global-admin flag change validation for the acting user/API key. */
export function withPatchGlobalAdminRefine<T extends z.ZodType<GlobalAdminPatchable>>(
  schema: T,
  actorIsGlobalAdmin: boolean
) {
  return schema.superRefine((val, ctx) =>
    refinePatchGlobalAdminFlag(val, ctx, actorIsGlobalAdmin)
  )
}
