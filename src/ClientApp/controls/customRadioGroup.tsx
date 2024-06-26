// components/CustomRadioGroup.tsx
import React, { useState, useEffect } from 'react'

interface CustomRadioOption {
  value: string
  label: string
}

interface CustomRadioGroupProps {
  options: CustomRadioOption[]
  initialValue?: string
  onChange?: (value: string) => void
  title?: string
  error?: string
  className?: string
  radioClassName?: string
  errorClassName?: string
  readOnly?: boolean
  disabled?: boolean
}

const CustomRadioGroup: React.FC<CustomRadioGroupProps> = ({
  options,
  initialValue,
  onChange,
  title,
  error,
  className = '',
  radioClassName = '',
  errorClassName = '',
  readOnly = false,
  disabled = false
}) => {
  const [selectedValue, setSelectedValue] = useState(initialValue || '')

  useEffect(() => {
    if (initialValue) {
      setSelectedValue(initialValue)
    }
  }, [initialValue])

  const handleOptionChange = (value: string) => {
    if (!readOnly && !disabled) {
      setSelectedValue(value)
      onChange?.(value)
    }
  }

  return (
    <div className={`flex flex-col ${className}`}>
      {title && <label className="mb-1">{title}</label>}
      <div className="flex flex-col">
        {options.map((option) => (
          <label
            key={option.value}
            className={`flex items-center mb-2 ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
          >
            <input
              type="radio"
              value={option.value}
              checked={selectedValue === option.value}
              onChange={() => handleOptionChange(option.value)}
              className={`mr-2 ${radioClassName}`}
              disabled={disabled}
            />
            {option.label}
          </label>
        ))}
      </div>
      {error && (
        <p className={`text-red-500 mt-1 ${errorClassName}`}>{error}</p>
      )}
    </div>
  )
}

export { CustomRadioGroup }
