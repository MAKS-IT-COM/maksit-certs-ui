import { ResponseModelBase } from '../../ResponseModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

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

export interface UserEntityScopeResponse {
  id: string
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
