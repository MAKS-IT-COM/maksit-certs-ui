import { ResponseModelBase } from '../../ResponseModelBase'

/** Certs API user payload (LetsEncryptServer UserResponse). */
export interface UserResponse extends ResponseModelBase {

  /** Master */
  id: string
  username?: string
  isActive?: boolean
  lastLogin?: string

  /** Two-factor */
  twoFactorEnabled?: boolean
  twoFactorRecoveryCodes?: string[]
  qrCodeUrl?: string
  recoveryCodesLeft?: number
}
