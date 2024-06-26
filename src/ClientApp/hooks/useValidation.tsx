import { useState, useEffect, useCallback } from 'react'

// Helper functions for validation
const isBypass = (value: any) => {
  return true
}

const isValidEmail = (email: string) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

const isValidPhoneNumber = (phone: string) => {
  const phoneRegex = /^\+?[1-9]\d{1,14}$/
  return phoneRegex.test(phone)
}

const isValidContact = (contact: string) => {
  return isValidEmail(contact) || isValidPhoneNumber(contact)
}

const isValidHostname = (hostname: string) => {
  const hostnameRegex = /^(?!:\/\/)([a-zA-Z0-9-_]{1,63}\.?)+[a-zA-Z]{2,6}$/
  return hostnameRegex.test(hostname)
}

// Props interface for useValidation hook
interface UseValidationProps<T> {
  initialValue: T
  validateFn: (value: T) => boolean
  errorMessage: string
  emptyFieldMessage?: string // Optional custom message for empty fields
  defaultResetValue?: T // Optional default reset value
}

// Custom hook for input validation
const useValidation = <T extends string | number | Date>(
  props: UseValidationProps<T>
) => {
  const {
    initialValue,
    validateFn,
    errorMessage,
    emptyFieldMessage = 'This field cannot be empty.', // Default message
    defaultResetValue
  } = props

  const [value, setValue] = useState<T>(initialValue)
  const [error, setError] = useState('')

  const handleChange = useCallback(
    (newValue: T) => {
      setValue(newValue)
      const stringValue =
        newValue instanceof Date
          ? newValue.toISOString()
          : newValue.toString().trim()
      if (stringValue === '') {
        setError(emptyFieldMessage)
      } else if (!validateFn(newValue)) {
        setError(errorMessage)
      } else {
        setError('')
      }
    },
    [emptyFieldMessage, errorMessage, validateFn]
  )

  useEffect(() => {
    handleChange(initialValue)
  }, [initialValue, handleChange])

  const reset = useCallback(() => {
    const resetValue =
      defaultResetValue !== undefined ? defaultResetValue : initialValue

    setValue(resetValue)
    const stringValue =
      resetValue instanceof Date
        ? resetValue.toISOString()
        : resetValue.toString().trim()
    if (stringValue === '') {
      setError(emptyFieldMessage)
    } else {
      setError('')
    }
  }, [defaultResetValue, initialValue, emptyFieldMessage])

  return { value, error, handleChange, reset }
}

export {
  useValidation,
  isBypass,
  isValidEmail,
  isValidPhoneNumber,
  isValidContact,
  isValidHostname
}
