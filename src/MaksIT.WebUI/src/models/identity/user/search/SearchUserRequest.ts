import { PagedRequest } from '../../../PagedRequest'

export interface SearchUserRequest extends PagedRequest {
    organizationFilters?: string
    aplicattionFilters?: string
}