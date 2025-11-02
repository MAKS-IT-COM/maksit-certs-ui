import { RequestModelBase } from './RequestModelBase'

export interface PagedRequest extends RequestModelBase {
    pageSize?: number
    pageNumber?: number
    filters?: string
    collectionFilters?: { [key: string]: string }
    sortBy?: string
    isAscending?: boolean
}