import { array, object, RefinementCtx, ZodType, z } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../PatchRequestModelBase'
import { guidStringSchemaOptionalNullable } from '../../guidString'
import { PatchUserEntityScopeRequest } from './PatchUserEntityScopeRequest'

/** Enable/disable 2FA (backend reads <c>twoFactorEnabled</c> directly). */
export interface PatchUserEnableTwoFactorRequest extends PatchRequestModelBase {
  twoFactorEnabled: boolean
}

/** Change password */
export interface PatchUserChangePasswordRequest extends PatchRequestModelBase {
  password: string
  confirmPassword?: string
}

const PatchUserChangePasswordRequestSchemaRefine = (data: PatchUserChangePasswordRequest, ctx: RefinementCtx) => {
  const password = data.password
  const passwordPolicy = [
    {
      regex: /.{8,}/,
      message: 'Password must be at least 8 characters long',
    },
    {
      regex: /[A-Z]/,
      message: 'Password must contain at least one uppercase letter',
    },
    {
      regex: /[a-z]/,
      message: 'Password must contain at least one lowercase letter',
    },
    {
      regex: /[0-9]/,
      message: 'Password must contain at least one number',
    },
    {
      regex: /[^A-Za-z0-9]/,
      message: 'Password must contain at least one special character',
    },
  ]

  passwordPolicy.forEach((rule) => {
    if (!rule.regex.test(password)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: rule.message,
        path: ['password'],
      })
    }
  })

  if (data.password !== data.confirmPassword) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Passwords do not match',
      path: ['confirmPassword'],
    })
  }
}

export const PatchUserChangePasswordRequestSchema: ZodType<PatchUserChangePasswordRequest> = PatchRequestModelBaseSchema.and(
  object({
    password: z.string(),
    confirmPassword: z.string().optional(),
  })
).superRefine(PatchUserChangePasswordRequestSchemaRefine)

/** Mirrors <c>MaksIT.CertsUI.Models.Identity.User.PatchUserRequest</c>. */
export interface PatchUserRequest extends PatchRequestModelBase {
  username?: string | null
  email?: string | null
  mobileNumber?: string | null
  isActive?: boolean | null
  password?: string | null
  twoFactorEnabled?: boolean | null
  isGlobalAdmin?: boolean | null
  entityScopes?: PatchUserEntityScopeRequest[] | null
}

export const PatchUserRequestSchema: z.Schema<PatchUserRequest> = PatchRequestModelBaseSchema.and(
  object({
    username: z.string().optional().nullable(),
    email: z.string().optional().nullable(),
    mobileNumber: z.string().optional().nullable(),
    isActive: z.boolean().optional().nullable(),
    password: z.string().optional().nullable(),
    twoFactorEnabled: z.boolean().optional().nullable(),
    isGlobalAdmin: z.boolean().optional().nullable(),
    entityScopes: array(object({
      id: guidStringSchemaOptionalNullable,
      entityId: guidStringSchemaOptionalNullable,
      entityType: z.number().optional().nullable(),
      scope: z.number().optional().nullable(),
    }).passthrough()).optional().nullable(),
  })
)
