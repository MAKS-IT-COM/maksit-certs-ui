/** Matches server <see cref="MaksIT.Core.Webapi.Models.PagedResponse{T}" /> (camelCase JSON). */
export interface PagedResponse<T> {
  data: T[]
  totalRecords: number
  pageNumber: number
  pageSize: number
}
