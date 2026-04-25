import type { DataTableColumn } from './DataTable'

const createColumn = <T, K extends keyof T>(col: DataTableColumn<T, K>): DataTableColumn<T, K> => {
  return col
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const createColumns = <T>(cols: DataTableColumn<T, any>[]) => {
  return cols as unknown as DataTableColumn<T, keyof T>[]
}

export { createColumn, createColumns }
