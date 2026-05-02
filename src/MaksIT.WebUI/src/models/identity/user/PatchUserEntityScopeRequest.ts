import { PatchRequestModelBase } from '../../PatchRequestModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

/** Mirrors <c>MaksIT.CertsUI.Models.Identity.User.PatchUserEntityScopeRequest</c>. */
export interface PatchUserEntityScopeRequest extends PatchRequestModelBase {
  id?: string | null
  entityId?: string | null
  entityType?: ScopeEntityType | null
  scope?: ScopePermission | null
}
