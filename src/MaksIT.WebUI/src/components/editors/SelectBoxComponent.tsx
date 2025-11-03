import { debounce } from 'lodash'
import { CircleX } from 'lucide-react'
import { ChangeEvent, FC, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { FieldContainer } from './FieldContainer'

export interface SelectBoxComponentOption {
  value: string | number
  label: string
}

interface SelectBoxComponentProps {
  label: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  errorText?: string
  options?: SelectBoxComponentOption[]

  // Field used to compare with the value
  idField?: string
  // Fields to search against when filtering options
  filterFields?: string[]
  // Callback function called with a filter string, debounced
  onFilterChange?: (filters: string) => void

  value?: string | number
  onChange?: (e: ChangeEvent<HTMLInputElement>) => void
  placeholder?: string
  readOnly?: boolean
  disabled?: boolean
}

const SelectBoxComponent: FC<SelectBoxComponentProps> = (props) => {

  const {
    label,
    colspan = 12,
    errorText,
    options = [],

    idField = 'id',
    filterFields,
    onFilterChange,

    value = '',
    onChange,
    placeholder,
    readOnly = false,
    disabled = false,
  } = props

  // Local state to control dropdown visibility and current filter text
  const [showDropdown, setShowDropdown] = useState(false)
  const [filterValue, setFilterValue] = useState<string>('')

  // Memoized debounced callback for filter changes.
  const debounceOnFilterChange = useMemo(() => {
    return onFilterChange ? debounce(onFilterChange, 500) : undefined
  }, [onFilterChange])

  // Refs to store previous values to detect changes
  const initRef = useRef(false)
  const prevValue = useRef(value)
  const prevFilterValue = useRef(filterValue)

  // Update the selected value and notify parent via onValueChange callback.
  const handleValueChange = useCallback(
    (newValue: string | number) => {
      prevValue.current = newValue
      // Simulate a ChangeEvent with the new value
      onChange?.({ target: { value: newValue } } as ChangeEvent<HTMLInputElement>)
    },
    [onChange]
  )

  // Handle input changes for filtering options.
  const handleFilterChange = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      if (disabled) return
      const newFilter = e.target.value
      setFilterValue(newFilter)

      // If filter value hasn't changed, exit early.
      if (prevFilterValue.current === newFilter) {
        return
      }

      // Build a filter query string based on the filterFields.
      const query = filterFields
        ?.map((field) => `${field}.Contains("${newFilter}")`)
        .filter(Boolean)
        .join(' || ') ?? ''

      // If debounced filter callback is provided, invoke it.
      if (debounceOnFilterChange) {
        debounceOnFilterChange(query)
      }

      // Clear the selected value when user types in filter.
      if (showDropdown) {
        handleValueChange('')
      }

      prevFilterValue.current = newFilter
    },
    [filterFields, debounceOnFilterChange, showDropdown, handleValueChange, disabled]
  )

  // Effect to sync external value with filter text and trigger filtering.
  useEffect(() => {
    // When value is cleared, also clear the filter.
    if (value === '') {
      if (prevValue.current !== value) {
        // Simulate clearing the filter input.
        handleFilterChange({ target: { value: '' } } as ChangeEvent<HTMLInputElement>)
      }
      return
    }

    // Find the option that matches the current value.
    const selectedOption = options.find((option) => option.value === value)
    if (selectedOption) {
      if (filterValue !== selectedOption.label) {
        setFilterValue(selectedOption.label) // Only update if the filterValue is different.
      }
      return
    }

    // If the value does not correspond to an existing option,
    // trigger filtering using the idField.
    if (debounceOnFilterChange && !initRef.current) {
      debounceOnFilterChange(`${idField} == "${value}"`)
      initRef.current = true
    }
  }, [value, filterValue, options, idField, debounceOnFilterChange, handleFilterChange])

  // Handle click on an option from the dropdown.
  const handleOptionClick = (optionValue: string | number) => {
    if (disabled) return
    // Update the selected value.
    handleValueChange(optionValue)
    // Update the input to display the selected option's label.
    const selectedOption = options.find((option) => option.value === optionValue)
    setFilterValue(selectedOption?.label ?? '')
    // Close the dropdown.
    setShowDropdown(false)
  }

  const actionButtons = () => {
    const className = 'p-1 text-gray-600 hover:text-gray-800 bg-white'
    if (disabled) return null
    return [
      !!filterValue && !readOnly && (
        <button
          key={'clear'}
          type={'button'}
          onClick={() => {
            setFilterValue('')
            handleValueChange('')
            if (debounceOnFilterChange) debounceOnFilterChange('')
          }}
          className={className}
          tabIndex={-1}
          aria-label={'Clear'}
        >
          <CircleX />
        </button>
      ),
    ].filter(Boolean)
  }

  return (
    <FieldContainer colspan={colspan} label={label} errorText={errorText}>
      <div className={'relative'}>

        <div className={'relative'}>
          <input
            type={'text'}
            value={filterValue}
            onChange={handleFilterChange}
            placeholder={placeholder}
            className={`shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline
        ${errorText ? 'border-red-500' : ''}
        ${disabled ? 'bg-gray-100 text-gray-500 cursor-default' : 'bg-white'}
        ${readOnly && !disabled ? 'text-gray-500 cursor-default' : ''}`}
            disabled={readOnly || disabled}
            // Open dropdown when input is focused.
            onFocus={() => { if (!disabled) setShowDropdown(true) }}
            // Delay closing dropdown to allow click events on options.
            onBlur={() => setTimeout(() => setShowDropdown(false), 200)}
          />

          {/* Action Buttons */}
          <div
            className={'absolute top-0 bottom-0 right-2 flex items-center gap-1 pointer-events-auto'}
          >
            {actionButtons()}
          </div>
        </div>

        {showDropdown && !disabled && (
          <div className={'absolute left-0 right-0 bg-white border border-gray-300 rounded mt-1 w-full shadow-lg z-10'}>
            {options.length > 0 ? (
              options.map((option) => (
                <div
                  key={option.value}
                  className={'px-4 py-2 cursor-pointer hover:bg-gray-200'}
                  onMouseDown={() => handleOptionClick(option.value)}
                >
                  {option.label}
                </div>
              ))
            ) : (
              <div className={'px-4 py-2 text-gray-500'}>No options found</div>
            )}
          </div>
        )}
      </div>
    </FieldContainer>
  )
}

export { SelectBoxComponent }