import { PatchAction } from '@/models/PatchAction'

export interface PatchHostnameRequest {
  hostname?: PatchAction<string>
  isDisabled?: PatchAction<boolean>
}
