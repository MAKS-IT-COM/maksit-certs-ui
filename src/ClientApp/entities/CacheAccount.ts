import { CacheAccountHostname } from './CacheAccountHostname'

export interface CacheAccount {
  accountId: string
  description?: string
  contacts: string[]
  hostnames: CacheAccountHostname[]
  isEditMode: boolean
}
