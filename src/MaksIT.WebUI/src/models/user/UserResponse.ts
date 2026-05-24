import type { ResponseModelBase } from '@maks-it.com/webui-contracts'
import { UserEntityScopeResponse } from './UserEntityScopeResponse'

/** Matches <c>MaksIT.CertsUI.Models.Identity.User.UserResponse</c> (camelCase JSON). */
export interface UserResponse extends ResponseModelBase {
  id: string
  username: string
  email?: string | null
  mobileNumber?: string | null
  isActive: boolean

  twoFactorEnabled?: boolean
  twoFactorRecoveryCodes?: string[]
  qrCodeUrl?: string
  recoveryCodesLeft?: number

  isGlobalAdmin?: boolean
  entityScopes?: UserEntityScopeResponse[]
}
