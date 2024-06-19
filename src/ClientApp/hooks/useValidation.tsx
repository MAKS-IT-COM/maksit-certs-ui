import { useState, useEffect } from "react"

// Helper functions for validation
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
interface UseValidationProps {
    initialValue: string
    validateFn: (value: string) => boolean
    errorMessage: string
}

// Custom hook for input validation
const useValidation = ({ initialValue, validateFn, errorMessage }: UseValidationProps) => {
    const [value, setValue] = useState(initialValue)
    const [error, setError] = useState("")

    const handleChange = (newValue: string) => {

        console.log(newValue)
        setValue(newValue)
        if (newValue.trim() === "") {
            setError("This field cannot be empty.")
        } else if (!validateFn(newValue.trim())) {
            setError(errorMessage)
        } else {
            setError("")
        }
    }

    useEffect(() => {
        handleChange(initialValue)
    }, [initialValue])

    return { value, error, handleChange, reset: () => setValue("") }
}

export { useValidation, isValidEmail, isValidPhoneNumber, isValidContact, isValidHostname }
