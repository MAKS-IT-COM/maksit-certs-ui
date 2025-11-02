import { useCallback, useEffect, useMemo, useState } from 'react'
import { Schema } from 'zod'
import { $ZodIssue } from 'zod/v4/core'
import { deepCopy } from '../functions/deep'

interface UseFormStateProps<FormState> {
  initialState: FormState;
  validationSchema: Schema<unknown>;
}

type IsPlainObject<T> = T extends object

// eslint-disable-next-line @typescript-eslint/no-unsafe-function-type
? T extends Function
  ? false
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  : T extends Array<any>
    ? false
    : true
: false;

type Decrement<N extends number> = [never, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9][N];

type Path<T, Depth extends number = 5> = Depth extends 0
? never
: T extends object
  ? IsPlainObject<T> extends true
    ? {
        [K in keyof T & string]: `${K}` | `${K}.${Path<T[K], Decrement<Depth>>}`;
      }[keyof T & string]
    : never
  : never;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const useFormState = <T extends Record<string, any>>(props: UseFormStateProps<T>) => {
  const {
    initialState,
    validationSchema
  } = props
  
  const [formState, setFormState] = useState(initialState)
  const [errors, setErrors] = useState<Partial<Record<Path<T>, string>>>({} as Partial<Record<Path<T>, string>>)
  const [formIsValid, setFormIsValid] = useState<boolean>(true)

  // Memoize the validation schema
  const memoizedValidationSchema = useMemo(() => validationSchema, [validationSchema])

  const validateForm = useCallback(() => {
    
    const validationResult = memoizedValidationSchema.safeParse(formState)

    setFormIsValid(validationResult.success)

    if (!validationResult.success) {
      const validationErrors = validationResult.error.issues

      const flattenErrors = (issues: $ZodIssue[]): Partial<Record<Path<T>, string>> => {
        const acc: Partial<Record<Path<T>, string>> = {}
        for (const issue of issues) {
          const path = issue.path.join('.') as Path<T>
          acc[path] = issue.message
        }
        return acc
      }

      const newErrors = flattenErrors(validationErrors)
      setErrors(newErrors)

      return
    }

    // Reset errors on successful validation
    setErrors(Object.keys(formState).reduce((acc, key) => ({
      ...acc,
      [key]: ''
    }), {} as Partial<Record<Path<T>, string>>))

  }, [formState, memoizedValidationSchema])

  useEffect(() => {
    validateForm()
  }, [formState, validateForm])

  /**
   * Handles input change for nested objects
   * @param key represents the path to the nested object property, max depth is 10
   * @param value represents the value to be set
   */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleInputChange = useCallback((key: Path<T, 10>, value: any) => {
    setFormState((prev) => {
      const newState = deepCopy(prev)

      const keys = key.split('.')
      const lastKey = keys.pop()!

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const nestedState = keys.reduce((acc, k) => acc[k], newState) as Record<string, any>
      nestedState[lastKey] = value

      return newState
    })

  }, [])

  const setInitialState = useCallback((newInitialState: T) => {
    setFormState(newInitialState)
  }, [])

  const setBulkState = useCallback((updater: (prev: T) => Partial<T>) => {
    setFormState(prev => ({ ...prev, ...updater(prev) }))
  }, [])

  return {
    formState,
    errors,
    formIsValid,
    handleInputChange,
    setInitialState,
    setBulkState
  }
}

export {
  useFormState
}