import { CacheAccount } from '@/entities/CacheAccount'
import { GetHostnameResponse } from './GetHostnameResponse'

export interface GetAccountResponse {
  accountId: string
  isDisabled: boolean
  description: string
  contacts: string[]
  challengeType?: string
  hostnames?: GetHostnameResponse[]
  isStaging: boolean
}

const toCacheAccount = (account: GetAccountResponse): CacheAccount => {
  return {
    accountId: account.accountId,
    isDisabled: account.isDisabled,
    description: account.description,
    contacts: account.contacts.map((contact) => contact),
    challengeType: account.challengeType,
    hostnames:
      account.hostnames?.map((hostname) => ({
        hostname: hostname.hostname,
        expires: new Date(hostname.expires),
        isUpcomingExpire: hostname.isUpcomingExpire,
        isDisabled: hostname.isDisabled
      })) ?? [],
    isStaging: account.isStaging,
    isEditMode: false
  }
}

export { toCacheAccount }
