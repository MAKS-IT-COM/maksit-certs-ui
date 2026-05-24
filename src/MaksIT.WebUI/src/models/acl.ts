import { parseAclEntries, parseAclEntry, type AclEntry } from '@maks-it.com/webui-core'
import { ScopeEntityType } from './ScopeEntityType'

/** JWT ACL entity-type codes for Certs UI (`I`, `K`). */
export const ACL_ENTITY_TYPE_MAP: Record<string, ScopeEntityType> = {
  I: ScopeEntityType.Identity,
  K: ScopeEntityType.ApiKey,
}

export const parseCertsAclEntry = (aclEntry: string) =>
  parseAclEntry(aclEntry, ACL_ENTITY_TYPE_MAP)

export const parseCertsAclEntries = (aclEntries: string[]) =>
  parseAclEntries(aclEntries, ACL_ENTITY_TYPE_MAP)

export type CertsAclEntry = AclEntry<ScopeEntityType>
