import { object, RefinementCtx, Schema, string } from 'zod'

export interface LoginRequest {
    username: string
    password: string
    twoFactorCode?: string
    twoFactorRecoveryCode?: string
}

const LoginRequestSchemaRefine = (data: LoginRequest, ctx: RefinementCtx) => {
  if (data.username === '') {
    ctx.addIssue({
      code: 'custom',
      message: 'Username cannot be empty',
      path: ['username']
    })
  }

  if (data.password === '') {
    ctx.addIssue({
      code: 'custom',
      message: 'Password cannot be empty',
      path: ['password']
    })
  }

  if (data.twoFactorCode && data.twoFactorRecoveryCode) {
    ctx.addIssue({
      code: 'custom',
      message: 'Cannot have both twoFactorCode and twoFactorRecoveryCode',
      path: ['twoFactorCode', 'twoFactorRecoveryCode']
    })
  }
}

export const LoginRequestSchema: Schema<LoginRequest> = object({
  username: string(),
  password: string(),
  twoFactorCode: string().optional(),
  twoFactorRecoveryCode: string().optional()
}).superRefine(LoginRequestSchemaRefine)