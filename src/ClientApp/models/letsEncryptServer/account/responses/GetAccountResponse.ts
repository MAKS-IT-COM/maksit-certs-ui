import { HostnameResponse } from './HostnameResponse'

export interface GetAccountResponse {
  accountId: string
  description?: string
  contacts: string[]
  challengeType?: string
  hostnames?: HostnameResponse[]
}
