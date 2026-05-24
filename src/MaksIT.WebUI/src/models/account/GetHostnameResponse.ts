import type { ResponseModelBase } from '@maks-it.com/webui-contracts'

export interface GetHostnameResponse extends ResponseModelBase {
  hostname: string
  expires: string
  isUpcomingExpire: boolean
  isDisabled: boolean
}
