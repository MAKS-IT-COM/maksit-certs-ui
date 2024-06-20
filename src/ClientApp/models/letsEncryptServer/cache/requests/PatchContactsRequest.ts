import { PatchAction } from '@/models/PatchAction'

export interface PatchContactsRequest {
  contacts: PatchAction<string>[]
}
