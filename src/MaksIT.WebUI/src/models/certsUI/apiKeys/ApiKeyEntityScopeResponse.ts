import { ResponseModelBase } from '../../ResponseModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.ApiKeyEntityScopeRsponse</c>. */
export interface ApiKeyEntityScopeResponse extends ResponseModelBase {
  id: string
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
