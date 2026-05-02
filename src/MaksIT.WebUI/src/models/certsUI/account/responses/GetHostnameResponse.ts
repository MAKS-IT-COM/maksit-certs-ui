import { ResponseModelBase } from '../../../ResponseModelBase'

export interface GetHostnameResponse extends ResponseModelBase {
  hostname: string
  expires: string
  isUpcomingExpire: boolean
  isDisabled: boolean
}
