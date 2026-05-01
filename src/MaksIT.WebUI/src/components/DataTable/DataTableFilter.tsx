import { useMemo, useState } from 'react'
import { debounce } from 'lodash'
import { postDataWithoutLoader } from '../../axiosConfig'
import { PagedRequest } from '../../models/PagedRequest'
import { PagedResponse } from '../../models/letsEncryptServer/common/PagedResponse'

interface FilterPropsBase {
  filterId?: string
  columnId: string
  accessorKey: string
  value?: FilterState
  disabled?: boolean
  onFilterChange?: (filterId: string, columnId: string, filters: string) => void
}

interface NormalFilterProps extends FilterPropsBase {
  type: 'normal',
}

interface RemoteFilterProps extends FilterPropsBase {
  type: 'remote',
  route: string
}

type FilterProps = NormalFilterProps | RemoteFilterProps

interface FilterState {
  value: string,
  operator: string
}

function toPascalCase(s: string): string {
  return s.length === 0 ? s : s.charAt(0).toUpperCase() + s.slice(1)
}

const DataTableFilter = <T extends { [key: string]: string }>(props: FilterProps) => {
  const {
    type,
    filterId = 'filters',
    columnId,
    accessorKey,
    value = {
      value: '',
      operator: 'contains'
    },
    disabled = false,
    onFilterChange
  } = props

  const [filterState, setFilterState] = useState<FilterState>(value)

  const debounceOnFilterChange = useMemo(() => {
    if (!onFilterChange) return

    return debounce((route: string, filters: string) => {

      postDataWithoutLoader<PagedRequest, PagedResponse<T>>(route, {
        pageSize: 100,
        filters
      }).then((response) => {
        if (!response.ok || !response.payload) return

        const rows = response.payload.data ?? []
        const linqQuery = rows.map(item => `${columnId} == "${item['id']}"`).join(' || ')
        onFilterChange?.(filterId, columnId, linqQuery)

      }).finally(() => {
      })
    }, 500)

  }, [filterId, columnId, onFilterChange])

  const handleFilterChange = (value: string, operator: string) => {
    setFilterState({
      value,
      operator
    })

    if (value === '') {
      onFilterChange?.(filterId, columnId, '')
      return
    }

    let linqQuery = ''

    const propName = toPascalCase(accessorKey)

    switch (operator) {
    case 'contains':
      linqQuery = `${propName}.Contains("${value}")`
      break
    case 'startsWith':
      linqQuery = `${propName}.StartsWith("${value}")`
      break
    case 'endsWith':
      linqQuery = `${propName}.EndsWith("${value}")`
      break
    case '=':
      linqQuery = `${propName} == "${value}"`
      break
    case '!=':
      linqQuery = `${propName} != "${value}"`
      break
    case '>':
      linqQuery = `${propName} > "${value}"`
      break
    case '<':
      linqQuery = `${propName} < "${value}"`
      break
    case '>=':
      linqQuery = `${propName} >= "${value}"`
      break
    case '<=':
      linqQuery = `${propName} <= "${value}"`
      break
    default:
      linqQuery = `${propName}.Contains("${value}")`
      break
    }

    if (type === 'normal') {
      onFilterChange?.(filterId, columnId, linqQuery)
    }

    if (type === 'remote' && debounceOnFilterChange) {
      const { route } = props as RemoteFilterProps

      debounceOnFilterChange(route, linqQuery)
    }
  }

  return (
    <div className={'flex w-full min-w-0 flex-col gap-1 overflow-hidden justify-center h-full'}>
      <input
        type={'text'}
        placeholder={'Filter...'}
        className={'block w-full min-w-0 max-w-full border rounded h-8 py-1 px-2 text-sm text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500/30 border-gray-300 bg-white disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-default'}
        value={filterState.value}
        onChange={e => handleFilterChange(e.target.value, filterState.operator)}
        disabled={disabled}
      />
      <select
        value={filterState.operator}
        onChange={e => handleFilterChange(filterState.value, e.target.value)}
        disabled={disabled}
        className={'block w-full min-w-0 max-w-full border rounded h-8 py-1 px-2 text-sm text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500/30 border-gray-300 bg-white disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-default'}
      >
        <option value={'contains'}>Contains</option>
        <option value={'startsWith'}>Starts With</option>
        <option value={'endsWith'}>Ends With</option>
        <option value={'='}>=</option>
        <option value={'!='}>!=</option>
        <option value={'>'}>{'>'}</option>
        <option value={'<'}>{'<'}</option>
        <option value={'>='}>{'>='}</option>
        <option value={'<='}>{'<='}</option>
      </select>
    </div>
  )
}

export {
  DataTableFilter
}
