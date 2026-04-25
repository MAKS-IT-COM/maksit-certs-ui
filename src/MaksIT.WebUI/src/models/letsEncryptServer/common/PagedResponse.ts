/** Matches server <see cref="MaksIT.Models.LetsEncryptServer.Common.PagedResponse{T}" /> (camelCase JSON). */
export interface PagedResponse<T> {
  data: T[]
  totalRecords: number
  pageNumber: number
  pageSize: number
}
