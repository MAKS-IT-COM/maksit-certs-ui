import { DataTableColumn } from './DataTable'

const createColumn = <T, K extends keyof T>(col: DataTableColumn<T, K>): DataTableColumn<T, K> => {
  return col
}

/**
 * TypeScript cannot express "an array of DataTableColumn<T, K> for various K" without using any or a cast in the helper.
 * This is a known limitation and your use of a helper like createColumns is the best practical solution.
 * @param cols 
 * @returns 
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const createColumns = <T>(cols: DataTableColumn<T, any>[]) => {
  return cols as unknown as DataTableColumn<T, keyof T>[]
}

export { createColumn, createColumns }