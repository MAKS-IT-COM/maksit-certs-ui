import {
  isValidISODateString,
  formatISODateString
} from './date'

import {
  deepCopy,
  deepDelta,
  deltaHasOperations,
  ENTITY_SCOPES_ARRAY_POLICY,
  deepEqual,
  deepMerge,
  deepPatternMatch
} from './deep'

import {
  enumToArr,
  enumToObj,
  enumToString,
  flagsToString,
  toggleFlag,
  hasFlag,
  hasAnyFlag
} from './enum'

import {
  isGuid
} from './isGuid'

import {
  parseAclEntry,
  parseAclEntries,
} from './acl'

import {
  saveBinaryToDisk
} from './file'

import {
  extractFilenameFromHeaders
} from './headers'

export {
  // date
  isValidISODateString,
  formatISODateString,

  // deep
  deepCopy,
  deepDelta,
  deltaHasOperations,
  ENTITY_SCOPES_ARRAY_POLICY,
  deepEqual,
  deepMerge,
  deepPatternMatch,

  // enum
  enumToArr,
  enumToObj,
  enumToString,
  flagsToString,
  toggleFlag,
  hasFlag,
  hasAnyFlag,

  // isGuid
  isGuid,

  // acl
  parseAclEntry,
  parseAclEntries,

  // file
  saveBinaryToDisk,

  // headers
  extractFilenameFromHeaders
}

export type { AclEntry } from './acl/parseAclEntry'