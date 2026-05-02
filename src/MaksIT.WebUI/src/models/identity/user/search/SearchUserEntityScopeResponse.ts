import { ResponseModelBase } from '../../../ResponseModelBase'
import { ScopeEntityType, ScopePermission } from '../../../engine/scopeEnums'

export interface SearchUserEntityScopeResponse extends ResponseModelBase {
  id: string
  userId: string
  username?: string
  entityId: string
  entityName?: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
