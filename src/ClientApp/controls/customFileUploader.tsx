'use client'
import React, { FC, useRef } from 'react'
import { CustomButton } from './customButton'

interface CustomFileUploaderProps {
  value?: File[] | null
  onChange?: (files: File[] | null) => void
  label?: string
  labelClassName?: string
  error?: string
  errorClassName?: string
  className?: string
  buttonClassName?: string
  inputClassName?: string
  readOnly?: boolean
  disabled?: boolean
  accept?: string
  multiple?: boolean
  title?: string
}

const CustomFileUploader: FC<CustomFileUploaderProps> = ({
  value,
  onChange,
  label,
  labelClassName = '',
  error,
  errorClassName = '',
  className = '',
  buttonClassName = '',
  inputClassName = '',
  readOnly = false,
  disabled = false,
  accept,
  multiple = false,
  title
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!readOnly && !disabled) {
      const files = e.target.files ? Array.from(e.target.files) : null
      onChange?.(files && files.length > 0 ? files : null)
      // Reset input value so the same file can be selected again
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <span className={`inline-flex items-center ${className}`}>
      {label && <label className={`mb-1 ${labelClassName}`}>{label}</label>}
      <input
        ref={fileInputRef}
        type="file"
        className="hidden"
        onChange={handleChange}
        disabled={disabled}
        readOnly={readOnly}
        accept={accept}
        multiple={multiple}
      />
      {value && value.length > 0 && (
        <>
          <span className="flex flex-row gap-2 mr-2 text-sm text-gray-600">
            {value.map((file, idx) => (
              <span key={file.name + idx}>{file.name}</span>
            ))}
          </span>
          <CustomButton
            onClick={() => {
              onChange?.(null)
              if (fileInputRef.current) fileInputRef.current.value = ''
            }}
            className={buttonClassName + ' ml-1'}
            disabled={disabled || readOnly}
            type="button"
          >
            ✕
          </CustomButton>
        </>
      )}
      <CustomButton
        onClick={() => {
          if (!disabled && !readOnly) fileInputRef.current?.click()
        }}
        className={buttonClassName}
        disabled={disabled}
      >
        {title}
      </CustomButton>
      {error && (
        <span className={`text-red-500 mt-1 ${errorClassName}`}>{error}</span>
      )}
    </span>
  )
}

export { CustomFileUploader }
