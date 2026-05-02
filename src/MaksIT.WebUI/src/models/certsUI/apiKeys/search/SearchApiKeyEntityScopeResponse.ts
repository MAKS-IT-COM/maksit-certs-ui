import { ResponseModelBase } from '../../../ResponseModelBase'
import { ScopeEntityType, ScopePermission } from '../../../engine/scopeEnums'

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
