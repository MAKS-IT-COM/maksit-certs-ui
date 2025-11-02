import { ScopeEntityType } from '../../models/identity/ScopeEntityType'
import { ScopePermission } from '../../models/identity/ScopePermissions'

export interface AclEntry {
  entityType: ScopeEntityType,
  entityId: string,
  scope: ScopePermission
}

const parseAclEntry = (aclEntry: string): AclEntry | null => {
  if (typeof aclEntry !== 'string')
    return null

  const parts = aclEntry.split(':')
  if (parts.length !== 3)

    return null

  const entityTypeMap: Record<string, ScopeEntityType> = {
    O: ScopeEntityType.Organization,
    A: ScopeEntityType.Application,
    S: ScopeEntityType.Secret,
    I: ScopeEntityType.Identity,
    K: ScopeEntityType.ApiKey
  }

  const entityType = entityTypeMap[parts[0]] ?? 'Unknown'
  const entityId = parts[1]
  const scopePermission = parseInt(parts[2], 16) as ScopePermission


  const aclEntryResult: AclEntry = {
    entityType,
    entityId,
    scope: scopePermission
  }

  console.log('Parsed ACL Entry:', aclEntryResult)

  return aclEntryResult
}

const parseAclEntries = (aclEntries: string[]): AclEntry [] => {
  return aclEntries
    .map(parseAclEntry)
    .filter((entry): entry is AclEntry => entry !== null)
}

export { parseAclEntry, parseAclEntries }
