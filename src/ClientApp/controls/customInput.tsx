// components/CustomInput.tsx
"use client"
import React from 'react'

interface CustomInputProps {
    value: string
    onChange: (value: string) => void
    placeholder?: string
    type: 'text' | 'password' | 'email' | 'number' | 'tel' | 'url'
    error?: string
    title?: string
    inputClassName?: string
    errorClassName?: string
    className?: string
}

const CustomInput: React.FC<CustomInputProps> = ({
    value,
    onChange,
    placeholder = '',
    type = 'text',
    error,
    title,
    inputClassName = '',
    errorClassName = '',
    className = ''
}) => {
    return (
        <div className={className}>
            {title && <label>{title}</label>}
            <input
                type={type}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                placeholder={placeholder}
                className={inputClassName}
            />
            {error && <p className={errorClassName}>{error}</p>}
        </div>
    )
}

export { CustomInput }
