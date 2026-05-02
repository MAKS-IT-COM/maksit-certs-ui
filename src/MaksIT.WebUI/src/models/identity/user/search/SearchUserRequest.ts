import { PagedRequest } from '../../../PagedRequest'

export interface SearchUserRequest extends PagedRequest {
  organizationFilters?: string
  applicationFilters?: string
}
