import type { ResponseModelBase } from '@maks-it.com/webui-contracts'
import { ScopeEntityType } from '../../ScopeEntityType'
import { ScopePermission } from '@maks-it.com/webui-core'

export interface SearchUserEntityScopeResponse extends ResponseModelBase {
  id: string
  userId: string
  username?: string
  entityId: string
  entityName?: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
