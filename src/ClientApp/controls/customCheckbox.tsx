// components/CustomCheckbox.tsx
import React, { FC } from 'react'

interface CustomCheckboxProps {
  checked: boolean
  onChange?: (checked: boolean) => void
  label?: string
  checkboxClassName?: string
  labelClassName?: string
  error?: string
  errorClassName?: string
  className?: string
  readOnly?: boolean
  disabled?: boolean
}

const CustomCheckbox: FC<CustomCheckboxProps> = (props) => {
  const {
    checked,
    onChange,
    label,
    checkboxClassName = '',
    labelClassName = '',
    error,
    errorClassName = '',
    className = '',
    readOnly = false,
    disabled = false
  } = props

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!readOnly && !disabled) {
      onChange?.(e.target.checked)
    }
  }

  return (
    <div className={`flex flex-col ${className}`}>
      <label className={`flex items-center ${labelClassName}`}>
        <input
          type="checkbox"
          checked={checked}
          onChange={handleChange}
          className={`mr-2 ${checkboxClassName}`}
          readOnly={readOnly}
          disabled={disabled}
        />
        {label && <span>{label}</span>}
      </label>
      {error && (
        <p className={`text-red-500 mt-1 ${errorClassName}`}>{error}</p>
      )}
    </div>
  )
}

export { CustomCheckbox }
