"use client"
import React, { FC } from 'react'

interface CustomButtonProps {
    onClick?: () => void
    className?: string
    children: React.ReactNode
    disabled?: boolean
    type?: "button" | "submit" | "reset"
}

const CustomButton: FC<CustomButtonProps> = (props) => {

    const { onClick, className = '', children, disabled = false, type = 'button' } = props

    return (
        <button
            onClick={onClick}
            className={className}
            disabled={disabled}
            type={type}
        >
            {children}
        </button>
    )
}

export { CustomButton }
