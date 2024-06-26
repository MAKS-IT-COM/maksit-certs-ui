// components/CustomInput.tsx
'use client'
import React, { FC } from 'react'

interface CustomInputProps {
  value: string
  onChange?: (value: string) => void
  placeholder?: string
  type: 'text' | 'password' | 'email' | 'number' | 'tel' | 'url'
  error?: string
  title?: string
  inputClassName?: string
  errorClassName?: string
  className?: string
  readOnly?: boolean
  disabled?: boolean
  children?: React.ReactNode // Added for additional elements
}

const CustomInput: FC<CustomInputProps> = ({
  value,
  onChange,
  placeholder = '',
  type = 'text',
  error,
  title,
  inputClassName = '',
  errorClassName = '',
  className = '',
  readOnly = false,
  disabled = false,
  children // Added for additional elements
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.value)
  }

  return (
    <div className={`flex flex-col ${className}`}>
      {title && <label className="mb-1">{title}</label>}
      <div className="flex items-center">
        <input
          type={type}
          value={value}
          onChange={handleChange}
          placeholder={placeholder}
          className={`flex-grow ${inputClassName}`}
          readOnly={readOnly}
          disabled={disabled}
        />
        {children && <div className="ml-2">{children}</div>}
      </div>
      {error && (
        <p className={`text-red-500 mt-1 ${errorClassName}`}>{error}</p>
      )}
    </div>
  )
}

export { CustomInput }
