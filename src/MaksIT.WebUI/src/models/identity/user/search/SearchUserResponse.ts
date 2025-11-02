import { ResponseModelBase } from '../../../ResponseModelBase'
import { Role } from '../../Role'

export interface SearchUserResponse extends ResponseModelBase {
    id: string

    username: string
    email:string
    mobileNumber: string
    isActive: boolean

    twoFactorEnabled: boolean
    recoveryCodesLeft?: number
  
    createdAt: string
    lastLogin?: string
  
    organizationId?: string
    organizationName?: string

    organizationRoleId?: string
    organizationRole?: Role

    applicationId?: string
    applicationName?: string

    applicationRoleId?: string
    applicationRole?: Role
}