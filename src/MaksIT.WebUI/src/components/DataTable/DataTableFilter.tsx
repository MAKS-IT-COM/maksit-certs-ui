import { useMemo, useState } from 'react'
import { debounce } from 'lodash'
import { useAppDispatch } from '../../redux/hooks'
import { postData } from '../../axiosConfig'
import { PagedRequest } from '../../models/PagedRequest'
import { PagedResponse } from '../../models/PagedResponse'

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

  const dispatch = useAppDispatch()

  const [filterState, setFilterState] = useState<FilterState>(value)

  const debounceOnFilterChange = useMemo(() => {
    if (!onFilterChange) return
   
    return debounce((route: string, filters: string) => {

      postData<PagedRequest, PagedResponse<T>>(route, {
        pageSize: 100,
        filters
      }).then((response) => {
        if (!response) return

        const linqQuery = response.items.map(item => `${columnId} == "${item['id']}"`).join(' || ')
        onFilterChange?.(filterId, columnId, linqQuery)
         
      }).finally(() => {
        
      })  
    }, 500)
   
  }, [filterId, columnId, dispatch, onFilterChange])

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

    switch (operator) {
    case 'contains':
      linqQuery = `${accessorKey}.Contains("${value}")`
      break
    case 'startsWith':
      linqQuery = `${accessorKey}.StartsWith("${value}")`
      break
    case 'endsWith':
      linqQuery = `${accessorKey}.EndsWith("${value}")`
      break
    case '=':
      linqQuery = `${accessorKey} == "${value}"`
      break
    case '!=':
      linqQuery = `${accessorKey} != "${value}"`
      break
    case '>':
      linqQuery = `${accessorKey} > "${value}"`
      break
    case '<':
      linqQuery = `${accessorKey} < "${value}"`
      break
    case '>=':
      linqQuery = `${accessorKey} >= "${value}"`
      break
    case '<=':
      linqQuery = `${accessorKey} <= "${value}"`
      break
    default:
      linqQuery = `${accessorKey}.Contains("${value}")` // Default case handles using 'contains' as the safe fallback
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
    <div className={'flex flex-col gap-2'}>
      <input
        type={'text'}
        placeholder={'Filter...'}
        className={'w-full border border-outline/20 rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:bg-gray-100 disabled:opacity-60'}
        value={filterState.value}
        onChange={e => handleFilterChange(e.target.value, filterState.operator)}
        disabled={disabled}
      />
      <select
        value={filterState.operator}
        onChange={e => handleFilterChange(filterState.value, e.target.value)}
        disabled={disabled}
        className={'w-full border border-outline/20 rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:bg-gray-100 disabled:opacity-60'}
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