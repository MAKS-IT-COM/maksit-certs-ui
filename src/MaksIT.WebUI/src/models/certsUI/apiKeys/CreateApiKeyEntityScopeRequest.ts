import { RequestModelBase } from '../../RequestModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.CreateApiKeyEntityScopeRequest</c>. */
export interface CreateApiKeyEntityScopeRequest extends RequestModelBase {
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
