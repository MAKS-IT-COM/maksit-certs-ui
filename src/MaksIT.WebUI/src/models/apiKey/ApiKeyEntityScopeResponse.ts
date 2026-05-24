import type { ResponseModelBase } from '@maks-it.com/webui-contracts'
import { ScopeEntityType } from '../ScopeEntityType'
import { ScopePermission } from '@maks-it.com/webui-core'

/** Mirrors <c>MaksIT.CertsUI.Models.APIKeys.ApiKeyEntityScopeRsponse</c>. */
export interface ApiKeyEntityScopeResponse extends ResponseModelBase {
  id: string
  entityId: string
  entityType: ScopeEntityType
  scope: ScopePermission
}
