import { object, RefinementCtx, ZodType, string, ZodIssueCode } from 'zod'

export interface LoginRequest {
    username: string
    password: string
    twoFactorCode?: string
    twoFactorRecoveryCode?: string
}

const LoginRequestSchemaRefine = (data: LoginRequest, ctx: RefinementCtx) => {

  if (data.username === '') {
    ctx.addIssue({
      code: ZodIssueCode.custom,
      message: 'Username cannot be empty',
      path: ['username']
    })
  }

  if (data.password === '') {
    ctx.addIssue({
      code: ZodIssueCode.custom,
      message: 'Password cannot be empty',
      path: ['password']
    })
  }

  if (data.twoFactorCode && data.twoFactorRecoveryCode) {
    ctx.addIssue({
      code: ZodIssueCode.custom,
      message: 'Cannot have both twoFactorCode and twoFactorRecoveryCode',
      path: ['twoFactorCode', 'twoFactorRecoveryCode']
    })
  }
}

export const LoginRequestSchema: ZodType<LoginRequest> = object({
  username: string(),
  password: string(),
  twoFactorCode: string().optional(),
  twoFactorRecoveryCode: string().optional()
}).superRefine(LoginRequestSchemaRefine)
