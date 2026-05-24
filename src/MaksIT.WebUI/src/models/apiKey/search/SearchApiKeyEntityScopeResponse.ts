import type { ResponseModelBase } from '@maks-it.com/webui-contracts'
import { ScopeEntityType } from '../../ScopeEntityType'
import { ScopePermission } from '@maks-it.com/webui-core'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.Search.SearchApiKeyEntityScopeResponse</c>. */
export interface SearchApiKeyEntityScopeResponse extends ResponseModelBase {
  id: string
  apiKeyId: string
  description?: string | null
  entityId: string
  entityName?: string | null
  entityType: ScopeEntityType
  scope: ScopePermission
}
