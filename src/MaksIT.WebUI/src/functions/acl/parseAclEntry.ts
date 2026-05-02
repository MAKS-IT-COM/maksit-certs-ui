import { ScopeEntityType, ScopePermission } from '../../models/engine/scopeEnums'

export interface AclEntry {
  entityType: ScopeEntityType
  entityId: string
  scope: ScopePermission
}

const parseAclEntry = (aclEntry: string): AclEntry | null => {
  if (typeof aclEntry !== 'string')
    return null

  const parts = aclEntry.split(':')
  if (parts.length !== 3)
    return null

  const entityTypeMap: Record<string, ScopeEntityType> = {
    I: ScopeEntityType.Identity,
    K: ScopeEntityType.ApiKey,
  }

  const entityType = entityTypeMap[parts[0]]
  if (entityType === undefined)
    return null

  const entityId = parts[1]
  const scopePermission = parseInt(parts[2], 16) as ScopePermission

  return {
    entityType,
    entityId,
    scope: scopePermission,
  }
}

const parseAclEntries = (aclEntries: string[]): AclEntry[] => {
  return aclEntries
    .map(parseAclEntry)
    .filter((entry): entry is AclEntry => entry !== null)
}

export { parseAclEntry, parseAclEntries }
