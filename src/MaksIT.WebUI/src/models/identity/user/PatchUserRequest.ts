import { object, RefinementCtx, ZodType, z } from 'zod'
import { PatchRequestModelBase, PatchRequestModelBaseSchema } from '../../PatchRequestModelBase'
import { PatchUserEntityScopeRequest, PatchUserEntityScopeRequestSchema } from './PatchUserEntityScopeRequest'


// Enable/Disable Two Factor
export interface PatchUserEnabeleTwoFactorRequest extends PatchRequestModelBase {
  twoFactorEnabled: boolean
}

// Change password
export interface PatchUserChangePasswordRequest extends PatchRequestModelBase {
    password: string,
    confirmPassword?: string 
}

const PatchUserChangePasswordRequestSchemaRefine = (data: PatchUserChangePasswordRequest, ctx: RefinementCtx) => {
  // Password policy validation
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

  // Password confirmation validation
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
    confirmPassword: z.string().optional()
  })
).superRefine(PatchUserChangePasswordRequestSchemaRefine)

// Update other parameters
export interface PatchUserRequest extends PatchRequestModelBase {
    username?: string
    email?: string
    mobileNumber?: string
    isActive?: boolean
    isGlobalAdmin?: boolean
    entityScopes?: PatchUserEntityScopeRequest[]
}

export const patchUserRequestProto: PatchUserRequest = {
  username:  '',
  email:  '',
  mobileNumber:  '',
  isActive: undefined,
  isGlobalAdmin: undefined,
  organizationRoles: undefined
}

export const PatchUserRequestSchema: z.Schema<PatchUserRequest> = PatchRequestModelBaseSchema.and(
  object({
    username: z.string().min(1).optional(),
    email: z.string().email().optional(),
    mobileNumber:  z.string().optional(),
    isActive: z.boolean().optional(),
    isGlobalAdmin: z.boolean().optional(),
    organizationRoles:  z.array(PatchUserEntityScopeRequestSchema).optional()
  })
)