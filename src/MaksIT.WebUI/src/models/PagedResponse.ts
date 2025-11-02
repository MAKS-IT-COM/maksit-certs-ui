import { ResponseModelBase } from './ResponseModelBase'

export interface PagedResponse<T> extends ResponseModelBase {
    items: T[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}