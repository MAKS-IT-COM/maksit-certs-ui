import { CacheAccountHostname } from './CacheAccountHostname'

export interface CacheAccount {
  accountId: string
  isDisabled: boolean
  description: string
  contacts: string[]
  challengeType?: string
  hostnames?: CacheAccountHostname[]
  isEditMode: boolean
  isStaging: boolean
}
