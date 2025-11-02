import { Eye, EyeOff } from 'lucide-react'
import { ChangeEvent, FC, useEffect, useRef, useState } from 'react'

interface TextBoxComponentProps {
  label: string
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12
  errorText?: string
  value?: string | number
  onChange?: (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => void
  type?: 'text' | 'password' | 'textarea' | 'number' | 'email' | 'time'
  placeholder?: string
  readOnly?: boolean
  disabled?: boolean
}

const TextBoxComponent: FC<TextBoxComponentProps> = (props) => {

  const {
    label,
    colspan = 12,
    errorText,
    value = '',
    onChange,
    type = 'text',
    placeholder,
    readOnly = false,
    disabled = false,
  } = props

  const prevValue = useRef<string | number>(value)

  useEffect(() => {
    prevValue.current = value
  }, [value])

  // Stato locale per gestire la visibilità della password
  const [showPassword, setShowPassword] = useState(false)

  const handleOnChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (prevValue.current === e.target.value)
      return

    prevValue.current = e.target.value

    onChange?.(e)
  }

  // Se il type è "textarea", comportamento invariato
  if (type === 'textarea') {
    return (
      <div className={`mb-4 ${colspan ? `col-span-${colspan}` : 'w-full'}`}>
        <label className={'block text-gray-700 text-sm font-bold mb-2'}>{label}</label>
        <textarea
          value={value}
          onChange={handleOnChange}
          placeholder={placeholder}
          className={`shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline ${
            errorText ? 'border-red-500' : ''
          } ${disabled ? 'bg-gray-100 text-gray-500 cursor-default' : 'bg-white'}${readOnly && !disabled ? ' text-gray-500 cursor-default' : ''}`}
          readOnly={readOnly}
          disabled={disabled}
        />
        {errorText && <p className={'text-red-500 text-xs italic mt-2'}>{errorText}</p>}
      </div>
    )
  }

  // Verifica se il valore non è vuoto (per tipo "password" useremo questa condizione)
  const hasContent = String(value).length > 0

  return (
    <div className={`mb-4 ${colspan ? `col-span-${colspan}` : 'w-full'}`}>
      <label className={'block text-gray-700 text-sm font-bold mb-2'}>{label}</label>

      {type === 'password' ? (
        // Wrapper che contiene input e bottone show/hide, ma bottone solo se c'è contenuto
        <div className={'relative'}>
          <input
            type={showPassword ? 'text' : 'password'}
            value={value}
            onChange={handleOnChange}
            placeholder={placeholder}
            className={`shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline ${
              errorText ? 'border-red-500' : ''
            } ${disabled ? 'bg-gray-100 text-gray-500 cursor-default' : 'bg-white'}${readOnly && !disabled ? ' text-gray-500 cursor-default' : ''}`}
            readOnly={readOnly}
            disabled={disabled}
          />
          {hasContent && (
            <button
              type={'button'}
              onClick={() => setShowPassword(prev => !prev)}
              className={'absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600'}
              tabIndex={-1} // Non interferisce con l'ordine di tabulazione
            >
              {showPassword ? <Eye /> : <EyeOff />}
            </button>
          )}
        </div>
      ) : (
        // Input normale per tutti gli altri tipi (text, number, email, time)
        <input
          type={type}
          value={value}
          onChange={handleOnChange}
          placeholder={placeholder}
          className={`shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline ${
            errorText ? 'border-red-500' : ''
          } ${disabled ? 'bg-gray-100 text-gray-500 cursor-default' : 'bg-white'}${readOnly && !disabled ? ' text-gray-500 cursor-default' : ''}`}
          readOnly={readOnly}
          disabled={disabled}
        />
      )}

      {errorText && <p className={'text-red-500 text-xs italic mt-2'}>{errorText}</p>}
    </div>
  )
}

export { TextBoxComponent }
