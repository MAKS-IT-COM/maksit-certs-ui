import { PatchAction } from '@/models/PatchAction'

export interface PatchAccountRequest {
  description?: PatchAction<string>
  contacts?: PatchAction<string>[]
}
