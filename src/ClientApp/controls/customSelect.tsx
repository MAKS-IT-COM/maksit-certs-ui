import React, { useRef, useEffect, useState } from 'react'
import { FaChevronDown, FaChevronUp } from 'react-icons/fa'

export interface CustomSelectOption {
  value: string
  label: string
}

export interface CustomSelectPropsBase {
  selectedValue: string | null | undefined
  onChange: (value: string) => void
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
      onChange(option.value)
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
      {title && <label className="mb-1">{title}</label>}
      <div
        className={`relative w-64 ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      >
        <div
          className={`p-2 border ${disabled ? 'border-gray-200' : 'border-gray-300'} rounded cursor-pointer flex justify-between items-center ${disabled ? 'cursor-not-allowed' : ''} ${selectBoxClassName}`}
          onClick={handleToggle}
        >
          {selectedOption ? selectedOption.label : 'Select an option'}
          {isOpen ? (
            <FaChevronUp className="ml-2" />
          ) : (
            <FaChevronDown className="ml-2" />
          )}
        </div>
        {isOpen && (
          <ul className="absolute z-10 w-full mt-1 overflow-y-auto bg-white border border-gray-300 max-h-60">
            {options.map((option) => (
              <li
                key={option.value}
                className={`p-2 hover:bg-gray-200 ${readOnly || disabled ? 'cursor-not-allowed' : 'cursor-pointer'}`}
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
