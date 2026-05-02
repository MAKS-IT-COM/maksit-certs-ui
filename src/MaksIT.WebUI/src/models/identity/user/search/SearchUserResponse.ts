import { ResponseModelBase } from '../../../ResponseModelBase'

/** Matches <c>MaksIT.CertsUI.Models.Identity.User.Search.SearchUserResponse</c>. */
export interface SearchUserResponse extends ResponseModelBase {
  id: string
  username: string
  email?: string | null
  mobileNumber?: string | null
  isActive: boolean

  twoFactorEnabled?: boolean
  recoveryCodesLeft?: number | null

  createdAt: string
  lastLogin?: string | null

  isGlobalAdmin?: boolean
  /** Present when the backend scopes rows by organization (used for ACL checks). */
  organizationId?: string | null
}
