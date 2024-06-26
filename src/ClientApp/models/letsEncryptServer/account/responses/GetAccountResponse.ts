import { HostnameResponse } from './HostnameResponse'

export interface GetAccountResponse {
  accountId: string
  isDisabled: boolean
  description: string
  contacts: string[]
  challengeType?: string
  hostnames?: HostnameResponse[]
  isStaging: boolean
}
