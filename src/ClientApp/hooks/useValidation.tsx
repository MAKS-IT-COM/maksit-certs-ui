import { useState, useEffect, useCallback } from 'react'

// Helper functions for validation
const isBypass = (value: any) => true
const isValidEmail = (email: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
const isValidPhoneNumber = (phone: string) => /^\+?[1-9]\d{1,14}$/.test(phone)
const isValidContact = (contact: string) =>
  isValidEmail(contact) || isValidPhoneNumber(contact)
const isValidHostname = (hostname: string) =>
  /^(?!:\/\/)([a-zA-Z0-9-_]{1,63}\.?)+[a-zA-Z]{2,6}$/.test(hostname)

// Props interface for useValidation hook
interface UseValidationProps<T> {
  externalValue: T
  setExternalValue: (value: T) => void
  validateFn: (value: T) => boolean
  errorMessage: string
  emptyFieldMessage?: string
  defaultValue: T
}

// Custom hook for input validation
const useValidation = <T extends string | number | Date | boolean>(
  props: UseValidationProps<T>
) => {
  const {
    externalValue,
    setExternalValue,
    validateFn,
    errorMessage,
    emptyFieldMessage = 'This field cannot be empty.',
    defaultValue
  } = props

  const [internalValue, setInternalValue] = useState(externalValue)
  const [error, setError] = useState('')

  const validate = useCallback(
    (value: T) => {
      const stringValue =
        value instanceof Date ? value.toISOString() : value.toString().trim()
      if (stringValue === '') {
        setError(emptyFieldMessage)
      } else if (!validateFn(value)) {
        setError(errorMessage)
      } else {
        setError('')
      }
    },
    [emptyFieldMessage, errorMessage, validateFn]
  )

  const handleChange = useCallback(
    (newValue: T) => {
      setInternalValue(newValue)
      setExternalValue(newValue)
      validate(newValue)
    },
    [setExternalValue, validate]
  )

  useEffect(() => {
    setInternalValue(externalValue)
    validate(externalValue)
  }, [externalValue, validate])

  const reset = useCallback(() => {
    setInternalValue(defaultValue)
    setError('')
  }, [defaultValue])

  return { value: internalValue, error, handleChange, reset }
}

export {
  useValidation,
  isBypass,
  isValidEmail,
  isValidPhoneNumber,
  isValidContact,
  isValidHostname
}
