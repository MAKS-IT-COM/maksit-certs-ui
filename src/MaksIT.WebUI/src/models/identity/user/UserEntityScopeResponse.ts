import { ScopeEntityType } from '../ScopeEntityType'
import { ScopePermission } from '../ScopePermissions'

export interface UserEntityScopeResponse {
    id: string,
    entityId: string,
    entityType: ScopeEntityType,
    scope: ScopePermission
}