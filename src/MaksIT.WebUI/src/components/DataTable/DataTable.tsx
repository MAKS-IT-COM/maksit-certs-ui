import React, { useState, useMemo, useRef, useEffect } from 'react'
import { AutoSizer, MultiGrid, GridCellProps } from 'react-virtualized'

import { PagedResponse } from '../../models/PagedResponse'
import { Plus, Trash2, Edit } from 'lucide-react'
import { debounce } from 'lodash'


interface FilterProps {
  columnId: string
}

interface CellProps<T, K extends keyof T = keyof T> {
  columnId: string
  data: T
  value: T[K]
}

export interface DataTableColumn<T, K extends keyof T = keyof T> {
  id: string
  accessorKey: K
  header: string
  filter: (
    props: FilterProps,
    onFilterChange: (filterId: string, columnId: string, filters: string) => void
  ) => React.ReactNode
  cell: (props: CellProps<T, K>) => React.ReactNode
}

interface DataTableProps<T> {
  rawd?: PagedResponse<T>
  columns: DataTableColumn<T>[]
  maxRecordsPerPage?: number

  idFields?: string[]

  allowAddRow?: () => boolean
  onAddRow?: () => void
  allowEditRow?: (ids: Record<string, string>) =>boolean
  onEditRow?: (ids: Record<string, string>) => void
  allowDeleteRow?: (ids: Record<string, string>) =>boolean
  onDeleteRow?: (ids: Record<string, string>) => void

  onFilterChange?: (filters: Record<string, string>) => void
  onPreviousPage?: (pageNumber: number) => void
  onNextPage?: (pageNumber: number) => void
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12

  storageKey?: string
}

const DEFAULT_ACTION_WIDTH = 80
const DEFAULT_COL_WIDTH = 150
const HEADER_ROWS = 2
const ROW_HEIGHT = 40

const DataTable = <T extends Record<string, unknown>,>(props: DataTableProps<T>) => {
  const {
    rawd,
    columns,
    idFields = ['id'],

    allowAddRow = () => false,
    onAddRow,
    allowEditRow = (_) => false,
    onEditRow,
    allowDeleteRow = (_) => false,
    onDeleteRow,

    onFilterChange,
    onPreviousPage,
    onNextPage,
    colspan = 12,
    storageKey,
  } = props

  const {
    items = [],
    pageNumber = 0,
    pageSize = 0,
    totalCount = 0,
    totalPages = 0,
    hasPreviousPage = false,
    hasNextPage = false,
  } = rawd || {}

  const gridRef = useRef<MultiGrid>(null)
  
  const [selectedRowIndex, setSelectedRowIndex] = useState<number | null>(null)
  const [filterValues, setFilterValues] = useState<Record<string, Record<string, string>>>({})

  const [colWidths, setColWidths] = useState<number[]>(() => {
    const defaultWidths = [DEFAULT_ACTION_WIDTH, ...columns.map(() => DEFAULT_COL_WIDTH)]

    if (storageKey) {
      try {
        const stored = localStorage.getItem(storageKey)
        if (stored) {
          const parsed = JSON.parse(stored)
          // Check if parsed is an array and matches the expected length
          if (Array.isArray(parsed) && parsed.length === columns.length + 1) {  // +1 for action column
            return parsed
          }
          else {
            // Remove invalid storage and fall back to default
            localStorage.removeItem(storageKey)
            return defaultWidths
          }
        }
      }
      catch {
        // Ignore and fall back to default
        return defaultWidths
      }
    }

    // If no storage key or no valid stored widths, return default
    return defaultWidths
  })

  useEffect(() => {
    if (storageKey) {
      localStorage.setItem(storageKey, JSON.stringify(colWidths))
    }
    if (gridRef.current) {
      gridRef.current.recomputeGridSize()
      gridRef.current.forceUpdateGrids()
    }
  }, [colWidths, storageKey])

  const debouncedOnFilterChange = useMemo(
    () => (onFilterChange ? debounce(onFilterChange, 500) : undefined),
    [onFilterChange]
  )

  const handleFilterChange = (
    filterId: string,
    columnId: string,
    filters: string
  ) => {
    setFilterValues((prev) => {
      const newValues = {
        ...prev,
        [filterId]: {
          ...prev[filterId],
          [columnId]: filters,
        },
      }

      const linqQueries = Object.fromEntries(
        Object.entries(newValues).map(([fid, cols]) => {
          const q = Object.values(cols)
            .filter((v) => v)
            .map((v) => `(${v})`)
            .join(' && ')
          return [fid, q]
        })
      )

      debouncedOnFilterChange?.(linqQueries)
      return newValues
    })
  }

  const handlePreviousPage = () => onPreviousPage?.(pageNumber - 1)
  const handleNextPage = () => onNextPage?.(pageNumber + 1)


  const getRealIdsFromRow = (rowIndex: number) => {
    const row = items[rowIndex]
    const ids = Object.fromEntries(
      idFields.map((key) => [key, `${row[key]}`])
    )
    
    return ids
  }

  const handleAddRow = () => onAddRow?.()
  const handleEditRow = (rowIndex: number) => {
    const ids = getRealIdsFromRow(rowIndex)

    onEditRow?.(ids)
  }
  const handleDeleteRow = (rowIndex: number) => {
    const ids = getRealIdsFromRow(rowIndex)

    onDeleteRow?.(ids)
  }

  const handleAllowAddRow = () => {
    return allowAddRow?.()
  }

  const handleAllowEditRow = (rowIndex: number) => {
    const ids = getRealIdsFromRow(rowIndex)
    return allowEditRow?.(ids)
  }

  const handleAllowDeleteRow = (rowIndex: number) => {
    const ids = getRealIdsFromRow(rowIndex)
    return allowDeleteRow?.(ids)
  }

  const handleRowClick = (idx: number) =>
    setSelectedRowIndex((prev) => (prev === idx ? null : idx))

  const handleHeaderResize = (colIdx: number, startX: number, startWidth: number) => {
    const onMouseMove = (e: MouseEvent) => {
      const delta = e.clientX - startX
      setColWidths(prev => {
        const next = [...prev]
        next[colIdx] = Math.max(40, startWidth + delta)
        return next
      })
    }
    const onMouseUp = () => {
      window.removeEventListener('mousemove', onMouseMove)
      window.removeEventListener('mouseup', onMouseUp)
    }
    window.addEventListener('mousemove', onMouseMove)
    window.addEventListener('mouseup', onMouseUp)
  }

  const cellRenderer = ({ columnIndex, key, rowIndex, style }: GridCellProps) => {
    const isActionCol = columnIndex === 0
    const col = columns[columnIndex - 1]

    // classi statiche; solo width/height via style
    const commonClasses = [
      'box-border',
      'flex',
      'items-center',
      'px-2',
      'py-1',
      'border-b',
      'border-r',
      'border-gray-200',
      'overflow-hidden',
      'whitespace-nowrap',
      'truncate',
      rowIndex >= HEADER_ROWS ? 'cursor-pointer' : '',
      rowIndex >= HEADER_ROWS && selectedRowIndex === rowIndex - HEADER_ROWS ? 'bg-sky-100' : '',
    ].filter(Boolean).join(' ')

    // Header
    if (rowIndex === 0) {
      const allowAddRowResult = handleAllowAddRow()

      if (isActionCol) {
        return (
          <div key={key} style={style} className={commonClasses}>
            <button
              onClick={handleAddRow}
              disabled={!allowAddRowResult}
              className={`p-1 ${allowAddRowResult
                ? 'cursor-pointer'
                : 'cursor-not-allowed opacity-50'
              }`}>
              <Plus />
            </button>
          </div>
        )
      }
      // il div esterno mantiene style/className, Resizable solo sul contenuto
      return (
        <div
          key={key}
          style={{ ...style, userSelect: 'none' }}
          className={commonClasses}
        >
          <span>{col.header}</span>
          <div
            style={{
              position: 'absolute',
              right: 0,
              top: 0,
              height: '100%',
              width: 8,
              cursor: 'col-resize',
              zIndex: 10,
              userSelect: 'none',
            }}
            onMouseDown={e => {
              e.preventDefault()
              handleHeaderResize(columnIndex, e.clientX, colWidths[columnIndex])
            }}
          />
        </div>
      )
    }

    // Filter row
    if (rowIndex === 1) {
      return isActionCol ? (
        <div key={key} style={style} className={commonClasses} />
      ) : (
        <div key={key} style={style} className={commonClasses}>
          {col.filter({ columnId: col.id }, handleFilterChange)}
        </div>
      )
    }

    // Data rows
    const dataIdx = rowIndex - HEADER_ROWS
    if (isActionCol) {
      const allowEditRowResult = handleAllowEditRow(dataIdx)
      const allowDeleteRowResult  = handleAllowDeleteRow(dataIdx)

      return (
        <div
          key={key}
          style={style}
          className={commonClasses} onClick={() => handleRowClick(dataIdx)}>
          <button
            onClick={e => {
              e.stopPropagation()
              handleEditRow(dataIdx)
            }}
            disabled={!allowEditRowResult}
            className={`p-1 ${allowEditRowResult
              ? 'cursor-pointer'
              : 'cursor-not-allowed opacity-50'
            }`}>
            <Edit />
          </button>
          <button
            onClick={e => {
              e.stopPropagation()
              handleDeleteRow(dataIdx)
            }}
            disabled={!allowDeleteRowResult}
            className={`p-1 ${allowDeleteRowResult
              ? 'cursor-pointer'
              : 'cursor-not-allowed opacity-50'
            }`}>
            <Trash2 />
          </button>
        </div>
      )
    }
    
    const row = items[dataIdx]

    return (
      <div
        key={key}
        style={style}
        className={commonClasses}
        onClick={() => setSelectedRowIndex(dataIdx)}
      >
        {col.cell({
          columnId: col.id,
          data: row,
          value: row[col.accessorKey]
        })}
      </div>
    )
  }

  // Handler for MultiGrid scroll
  const handleGridScroll = ({
    scrollTop,
    clientHeight,
    scrollHeight,
  }: {
    scrollTop: number
    clientHeight: number
    scrollHeight: number
  }) => {
    // At bottom
    if (scrollTop + clientHeight >= scrollHeight - 2 && hasNextPage) {
      handleNextPage()
    }
    // At top
    if (scrollTop <= 2 && hasPreviousPage) {
      handlePreviousPage()
    }
  }

  return (
    <div className={`col-span-${colspan} flex flex-col h-full w-full`}>
      <div className={'flex-1'}>
        <AutoSizer>
          {({ height, width }) => (
            <MultiGrid
              ref={gridRef}
              cellRenderer={cellRenderer}
              columnCount={columns.length + 1}
              columnWidth={({ index }) => colWidths[index]}
              fixedColumnCount={1}
              fixedRowCount={HEADER_ROWS}
              height={height}
              rowCount={items.length + HEADER_ROWS}
              rowHeight={({ index }) => index === 1 ? ROW_HEIGHT * 2 : ROW_HEIGHT}
              width={width}
              onScroll={({ scrollTop, clientHeight, scrollHeight }) =>
                handleGridScroll({ scrollTop, clientHeight, scrollHeight })
              }
            />
          )}
        </AutoSizer>
      </div>
      <div className={'mt-4 text-sm'}>
        <div className={'flex justify-end gap-4'}>
          <span>Page Size: {pageSize}</span>
          <span>Total Pages: {totalPages}</span>
          <span>Total Count: {totalCount}</span>
        </div>
        <div className={'flex items-center justify-between mt-2'}>
          <button
            onClick={handlePreviousPage}
            disabled={!hasPreviousPage}
            className={'px-4 py-2 bg-gray-200 rounded-md hover:bg-gray-300 aria-disabled:opacity-50'}
            aria-disabled={!hasPreviousPage}
          >
            Previous
          </button>
          <span>Page {pageNumber} of {totalPages}</span>
          <button
            onClick={handleNextPage}
            disabled={!hasNextPage}
            className={'px-4 py-2 bg-gray-200 rounded-md hover:bg-gray-300 aria-disabled:opacity-50'}
            aria-disabled={!hasNextPage}
          >
            Next
          </button>
        </div>
      </div>
    </div>
  )
}

export { DataTable }
