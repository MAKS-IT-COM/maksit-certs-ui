import { PagedResponse } from '../models/certsUI/common/PagedResponse'

/** Virtualized DataTable view model (client paging/search helpers). */
export interface DataTablePageView<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

/** Maps Certs API paging (`data`, `totalRecords`) to the shape DataTable expects. */
export function mapCertsPagedToDataTable<T>(raw: PagedResponse<T> | undefined): DataTablePageView<T> {
  if (raw == null || !Array.isArray(raw.data)) {
    return {
      items: [],
      pageNumber: 1,
      pageSize: 0,
      totalCount: 0,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    }
  }
  const { data, totalRecords, pageNumber, pageSize } = raw
  const totalPages = Math.max(1, pageSize > 0 ? Math.ceil(totalRecords / pageSize) : 1)
  return {
    items: data,
    pageNumber,
    pageSize,
    totalCount: totalRecords,
    totalPages,
    hasPreviousPage: pageNumber > 1,
    hasNextPage: pageNumber < totalPages,
  }
}
