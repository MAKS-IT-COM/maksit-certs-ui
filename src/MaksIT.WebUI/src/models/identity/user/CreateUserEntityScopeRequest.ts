import { RequestModelBase } from '../../RequestModelBase'
import { ScopeEntityType, ScopePermission } from '../../engine/scopeEnums'

/** Mirrors <c>MaksIT.CertsUI.Models.Identity.User.CreateUserEntityScopeRequest</c>. */
export interface CreateUserEntityScopeRequest extends RequestModelBase {
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
