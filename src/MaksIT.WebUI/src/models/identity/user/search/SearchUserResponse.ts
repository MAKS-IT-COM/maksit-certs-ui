import { ResponseModelBase } from '../../../ResponseModelBase'

export interface SearchUserResponse extends ResponseModelBase {
  id: string
  username: string
  isActive?: boolean
  lastLogin?: string
  twoFactorEnabled?: boolean
}
