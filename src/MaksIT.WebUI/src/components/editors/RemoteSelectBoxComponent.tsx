import { useState, useCallback, ChangeEvent, useEffect, useRef } from 'react'
import { SelectBoxComponent } from '.'
import { PagedRequest } from '../../models/PagedRequest'
import { ApiRoutes, GetApiRoute } from '../../AppMap'
import { useAppDispatch } from '../../redux/hooks'
import { postData } from '../../axiosConfig'
import { disableLoader, enableLoader } from '../../redux/slices/loaderSlice'
import { PagedResponse } from '../../models/PagedResponse'
import { SearchResponseBase } from '../../models/SearchResponseBase'
import { deepEqual } from '../../functions'

interface RemoteSelectBoxProps<TRequest extends PagedRequest> {
  apiRoute: ApiRoutes
  additionalFilters?: TRequest

  label: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  errorText?: string
   
  // Field used to compare with the value
  idField?: string
  /** Property on each search hit used as the option label (default {@code name}). */
  labelField?: string
  // Fields to search against when filtering options
  filterFields?: string[]

  value?: string | number
  onChange: (e: ChangeEvent<HTMLInputElement>) => void
  placeholder?: string
  readOnly?: boolean
}

const RemoteSelectBoxComponent = <TRequest extends PagedRequest>(props: RemoteSelectBoxProps<TRequest>) => {

  const {
    apiRoute,
    additionalFilters,

    label,
    colspan = 12,
    errorText,

    idField = 'id',
    labelField = 'name',
    filterFields = ['name'],

    value = '',
    onChange,
    placeholder,
    readOnly = false,
  } = props

  const dispatch = useAppDispatch()

  const [options, setOptions] = useState<SearchResponseBase []>([])

  const prevPagedRequest = useRef<TRequest | null>(null)

  const handleFilterChange = useCallback((filters?: string,  showLoader: boolean = false) => {
    const pagedRequest = {
      pageSize: 10,
      filters,
      ...additionalFilters
    } as TRequest

    if (deepEqual(pagedRequest, prevPagedRequest.current))
      return

    prevPagedRequest.current = pagedRequest

    if (!showLoader)
      dispatch(disableLoader())

    postData<TRequest, PagedResponse<SearchResponseBase>>(GetApiRoute(apiRoute).route, pagedRequest)
      .then((response) => {
        if (!response.ok || !response.payload) return
        setOptions(response.payload.items)
      })
      .catch((error) => {
        console.error('RemoteSelectBox fetch error:', error)
      })
      .finally(() => {
        dispatch(enableLoader())
      })
  }, [apiRoute, additionalFilters, dispatch])

  // Initialize options on mount
  useEffect(() => {
    handleFilterChange(undefined, true)
  }, [handleFilterChange])


  const handleOnChange = (e: ChangeEvent<HTMLInputElement>) => {
    onChange?.(e)
  }

  return <SelectBoxComponent
    colspan={colspan}
    label={label}
    placeholder={placeholder}
    options={options?.map(item => {
      const row = item as Record<string, unknown>
      const labelRaw = row[labelField] ?? row.name ?? row.id
      return {
        value: item.id,
        label: labelRaw != null ? String(labelRaw) : item.id
      }
    })}
    idField={idField}
    filterFields={filterFields}
    onFilterChange={(text) => handleFilterChange(text, false)}
    value={value}
    onChange={handleOnChange}
    errorText={errorText}
    readOnly={readOnly}
  />
}

export {
  RemoteSelectBoxComponent 
}