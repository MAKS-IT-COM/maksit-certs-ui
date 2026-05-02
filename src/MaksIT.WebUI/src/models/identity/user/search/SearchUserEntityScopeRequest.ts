import { PagedRequest } from '../../../PagedRequest'

export interface SearchUserEntityScopeRequest extends PagedRequest {
  userId?: string
}
