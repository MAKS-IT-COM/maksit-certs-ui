import { useState, useEffect } from "react"

// Helper functions for validation
const isValidEmail = (email: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
}

const isValidHostname = (hostname: string) => {
    const hostnameRegex = /^(?!:\/\/)([a-zA-Z0-9-_]{1,63}\.?)+[a-zA-Z]{2,6}$/
    return hostnameRegex.test(hostname)
}

// Custom hook for input validation
const useValidation = (initialValue: string, validateFn: (value: string) => boolean, errorMessage: string) => {
    const [value, setValue] = useState(initialValue)
    const [error, setError] = useState("")

    const handleChange = (newValue: string) => {
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

    return { value, error, handleChange }
}

export { useValidation, isValidEmail, isValidHostname }
