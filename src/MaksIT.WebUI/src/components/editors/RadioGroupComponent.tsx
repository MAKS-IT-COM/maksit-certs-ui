import React, { useEffect, useRef, useState } from 'react'
import { FieldContainer } from './FieldContainer'

interface RadioOption {
  value: string
  label: string
}

interface RadioGroupComponentProps {
  options: RadioOption[]
  label?: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  value?: string
  onChange?: (value: string) => void
  errorText?: string
  readOnly?: boolean
  disabled?: boolean
}

const RadioGroupComponent: React.FC<RadioGroupComponentProps> = (props) => {
  const {
    options,
    label,
    colspan = 6,
    value = '',
    onChange,
    errorText,
    readOnly = false,
    disabled = false
  } = props

  const prevValue = useRef<string>(value)
  const [selectedValue, setSelectedValue] = useState<string>(value)

  useEffect(() => {
    prevValue.current = value
    setSelectedValue(value)
  }, [value])

  const handleOptionChange = (val: string) => {
    if (readOnly || disabled) return
    if (prevValue.current === val) return
    prevValue.current = val
    setSelectedValue(val)
    onChange?.(val)
  }

  return (
    <FieldContainer colspan={colspan} label={label} errorText={errorText}>
      <div className={'flex flex-col'}>
        {options.map(option => {
          // Use default cursor (arrow) if disabled or readOnly, else pointer
          const isInactive = disabled || readOnly
          return (
            <label
              key={option.value}
              className={`flex items-center mb-2 ${disabled ? 'opacity-50' : ''} ${isInactive ? 'cursor-default' : 'cursor-pointer'}`}
            >
              <input
                type={'radio'}
                value={option.value}
                checked={selectedValue === option.value}
                onChange={() => handleOptionChange(option.value)}
                className={'mr-2'}
                disabled={disabled}
                readOnly={readOnly}
              />
              {option.label}
            </label>
          )
        })}
      </div>
    </FieldContainer>
  )
}

export { RadioGroupComponent }
