import { ResponseModelBase } from '../../ResponseModelBase'
import { UserEntityScopeResponse } from './UserEntityScopeResponse'


export interface UserResponse extends ResponseModelBase {
  id: string
  username: string
  email: string
  mobileNumber?: string
  isActive: boolean

  twoFactorEnabled: boolean
  twoFactorRecoveryCodes?: string[]
  twoFactorSharedKey?: string
  qrCodeUrl?: string
  recoveryCodesLeft?: number

  isGlobalAdmin: boolean
  entityScopes?: UserEntityScopeResponse[]
}