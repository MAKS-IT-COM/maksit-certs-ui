import { object, RefinementCtx, ZodType, z } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../PatchRequestModelBase'

/** Enable/disable 2FA (sent without other patch fields; Vault parity). */
export interface PatchUserEnabeleTwoFactorRequest extends PatchRequestModelBase {
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

/** User patch (Certs API: active flag and 2FA toggle use operations where applicable). */
export interface PatchUserRequest extends PatchRequestModelBase {
  isActive?: boolean
  twoFactorEnabled?: boolean
}

export const PatchUserRequestSchema: z.Schema<PatchUserRequest> = PatchRequestModelBaseSchema.and(
  object({
    isActive: z.boolean().optional(),
    twoFactorEnabled: z.boolean().optional(),
  })
)
