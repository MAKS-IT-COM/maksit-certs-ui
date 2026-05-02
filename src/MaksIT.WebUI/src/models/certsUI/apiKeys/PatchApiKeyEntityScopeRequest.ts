import { PatchRequestModelBase } from '../../PatchRequestModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.PatchApiKeyEntityScopeRequest</c>. */
export interface PatchApiKeyEntityScopeRequest extends PatchRequestModelBase {
  id?: string | null
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
