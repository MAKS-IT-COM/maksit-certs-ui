import { PatchAction } from '@/models/PatchAction'
import { PatchHostnameRequest } from './PatchHostnameRequest'

export interface PatchAccountRequest {
  description?: PatchAction<string>
  isDisabled?: PatchAction<boolean>
  contacts?: PatchAction<string>[]
  hostnames?: PatchHostnameRequest[]
}
