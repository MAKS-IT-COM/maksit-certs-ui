import { PagedRequest } from '../../../PagedRequest'

/** Certs API — no org/app filters (see server SearchAPIKeyRequest). */
export interface SearchAPIKeyRequest extends PagedRequest {
  descriptionFilter?: string
}
