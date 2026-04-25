import { ReactNode } from 'react'

/** Column definition for Vault-style dense list tables (MaksIT.Vault–style admin lists). */
export interface VaultStyleColumn<T> {
  id: string
  header: string
  /** Optional width hint, e.g. w-24, w-1/4 */
  widthClass?: string
  headerClassName?: string
  cellClassName?: string
  cell: (row: T) => ReactNode
}

export interface VaultStyleDataTableProps<T> {
  columns: VaultStyleColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  emptyMessage: string
  /** When true, no outer card chrome (use inside {@link VaultStyleListSection}). */
  embedded?: boolean
}

/**
 * Dense bordered table: neutral header bar, row dividers, hover — same structural pattern as Vault list views.
 */
function VaultStyleDataTable<T>(props: VaultStyleDataTableProps<T>) {
  const { columns, rows, rowKey, loading, emptyMessage, embedded } = props

  const wrapClass = embedded
    ? 'overflow-x-auto bg-white'
    : 'overflow-x-auto overflow-y-hidden rounded-md border border-neutral-200 bg-white shadow-sm'

  return (
    <div className={wrapClass}>
        <table className={'w-full min-w-[32rem] table-fixed border-collapse text-left text-sm'}>
          <thead>
            <tr className={'border-b border-neutral-200 bg-neutral-100'}>
              {columns.map((col) => (
                <th
                  key={col.id}
                  scope={'col'}
                  className={`px-3 py-2.5 text-xs font-semibold uppercase tracking-wide text-neutral-600 ${col.widthClass ?? ''} ${col.headerClassName ?? ''}`}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className={'divide-y divide-neutral-200'}>
            {loading ? (
              <tr>
                <td colSpan={columns.length} className={'px-3 py-10 text-center text-neutral-500'}>
                  Loading…
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className={'px-3 py-10 text-center text-neutral-500'}>
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={rowKey(row)} className={'transition-colors hover:bg-neutral-50'}>
                  {columns.map((col) => (
                    <td key={col.id} className={`px-3 py-2.5 align-middle text-neutral-900 ${col.cellClassName ?? ''}`}>
                      {col.cell(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
    </div>
  )
}

export { VaultStyleDataTable }
