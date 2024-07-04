import { PatchAction } from '@/models/PatchAction'

export interface PatchHostnameRequest {
  hostname?: PatchAction<string>
  isDisabled?: PatchAction<boolean>
}

export interface PatchAccountRequest {
  description?: PatchAction<string>
  isDisabled?: PatchAction<boolean>
  contacts?: PatchAction<string>[]
  hostnames?: PatchHostnameRequest[]
}
