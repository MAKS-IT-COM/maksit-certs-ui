
import { PatchAccountRequest } from '../models/letsEncryptServer/account/requests/PatchAccountRequest'
import { PatchOperation } from '../models/PatchOperation'
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

const toPatchAccountRequest = (account: CacheAccount): PatchAccountRequest => {
  return {
    description: { op: PatchOperation.None, value: account.description },
    isDisabled: { op: PatchOperation.None, value: account.isDisabled },
    contacts: account.contacts.map((contact, index) => ({
      index: index,
      op: PatchOperation.None,
      value: contact
    })),
    hostnames: account.hostnames?.map((hostname, index) => ({
      hostname: {
        index: index,
        op: PatchOperation.None,
        value: hostname.hostname
      },
      isDisabled: {
        index: index,
        op: PatchOperation.None,
        value: hostname.isDisabled
      }
    }))
  }
}

export { toPatchAccountRequest }
