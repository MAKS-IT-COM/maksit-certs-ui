import { HostnameResponse } from './HostnameResponse'

export interface GetAccountResponse {
  accountId: string
  contacts: string[]
  hostnames: HostnameResponse[]
}
