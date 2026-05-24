import { ScopeEntityType } from '../ScopeEntityType'
import { ScopePermission } from '@maks-it.com/webui-core'

export interface UserEntityScopeResponse {
  id: string
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
