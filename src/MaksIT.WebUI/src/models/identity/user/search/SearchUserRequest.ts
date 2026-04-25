import { PagedRequest } from '../../../PagedRequest'

/** Certs API — no org/app filters (see server SearchUserRequest). */
export interface SearchUserRequest extends PagedRequest {
  usernameFilter?: string
}