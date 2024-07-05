import React, { useRef, useEffect, useState } from 'react'
import { FaChevronDown, FaChevronUp } from 'react-icons/fa'

export interface CustomSelectOption {
  value: string
  label: string
}

export interface CustomSelectPropsBase {
  selectedValue: string | null | undefined
  onChange?: (value: string) => void
  readOnly?: boolean
  disabled?: boolean
  title?: string
  error?: string
  className?: string
  selectBoxClassName?: string
  errorClassName?: string
}

interface CustomSelectProps extends CustomSelectPropsBase {
  options: CustomSelectOption[]
}

const CustomSelect: React.FC<CustomSelectProps> = ({
  options,
  selectedValue,
  onChange,
  readOnly = false,
  disabled = false,
  title,
  error,
  className = '',
  selectBoxClassName = '',
  errorClassName = ''
}) => {
  const [isOpen, setIsOpen] = useState(false)
  const selectBoxRef = useRef<HTMLDivElement>(null)

  const handleToggle = () => {
    if (!readOnly && !disabled) {
      setIsOpen(!isOpen)
    }
  }

  const handleOptionClick = (option: CustomSelectOption) => {
    if (!readOnly && !disabled) {
      onChange?.(option.value)
      setIsOpen(false)
    }
  }

  const handleClickOutside = (event: MouseEvent) => {
    if (
      selectBoxRef.current &&
      !selectBoxRef.current.contains(event.target as Node)
    ) {
      setIsOpen(false)
    }
  }

  useEffect(() => {
    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [])

  const selectedOption =
    options.find((option) => option.value === selectedValue) || null

  return (
    <div className={`flex flex-col ${className}`} ref={selectBoxRef}>
      {title && <label className="mb-1 text-gray-700">{title}</label>}
      <div
        className={`relative w-64 ${disabled ? 'opacity-50 cursor-not-allowed' : readOnly ? 'cursor-not-allowed' : ''}`}
      >
        <div
          className={`p-2 border rounded flex justify-between items-center ${
            disabled
              ? 'border-gray-200 bg-gray-100 text-gray-500'
              : readOnly
                ? 'border-gray-300 bg-white cursor-not-allowed'
                : 'border-gray-300 bg-white cursor-pointer hover:border-gray-400'
          } ${selectBoxClassName}`}
          onClick={handleToggle}
        >
          <span className={`${disabled ? 'text-gray-500' : ''}`}>
            {selectedOption ? selectedOption.label : 'Select an option'}
          </span>

          {isOpen ? (
            <FaChevronUp
              className={`ml-2 ${disabled ? 'text-gray-500' : ''}`}
            />
          ) : (
            <FaChevronDown
              className={`ml-2 ${disabled ? 'text-gray-500' : ''}`}
            />
          )}
        </div>

        {isOpen && (
          <ul className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded shadow-lg max-h-60 overflow-y-auto">
            {options.map((option) => (
              <li
                key={option.value}
                className={'p-2'}
                onClick={() => handleOptionClick(option)}
              >
                {option.label}
              </li>
            ))}
          </ul>
        )}
      </div>

      {error && (
        <p className={`text-red-500 mt-1 ${errorClassName}`}>{error}</p>
      )}
    </div>
  )
}

export { CustomSelect }
